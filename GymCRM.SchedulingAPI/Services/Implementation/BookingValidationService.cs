using GymCRM.SchedulingAPI.Constants;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Models.Enums;
using GymCRM.SchedulingAPI.Services.Interface;

namespace GymCRM.SchedulingAPI.Services.Implementation;

public class BookingValidationService : IBookingValidationService
{
    private const int BufferBetweenTrainingSessions = 15;
    private const int SlotGranularityMinutes = 15;
    private static readonly int[] ValidDurationsMinutes = { 30, 60, 90 };

    private readonly ITrainerAvailabilitiesService _trainerAvailabilitiesService;
    private readonly ITrainingSessionsService _trainingSessionsService;
    private readonly ITimeOffService _timeOffService;
    private readonly IHolidayService _holidayService;

    public BookingValidationService(
        ITrainerAvailabilitiesService trainerAvailabilitiesService,
        ITrainingSessionsService trainingSessionsService,
        ITimeOffService timeOffService,
        IHolidayService holidayService)
    {
        _trainerAvailabilitiesService = trainerAvailabilitiesService;
        _trainingSessionsService = trainingSessionsService;
        _timeOffService = timeOffService;
        _holidayService = holidayService;
    }
    
    public async Task<ValidationResult> ValidateBookingAsync(
        InsertTrainingSession booking,
        CancellationToken cancellationToken = default,
        Guid? excludeSessionId = null)
    {
        if (booking is null)
        {
            throw new ArgumentNullException(nameof(booking));
        }

        if (booking.StartTime < DateTime.UtcNow
            || booking.EndTime < DateTime.UtcNow)
        {
            return ValidationResult.Fail(ValidationMessages.BookingInPast);
        }
        
        if (await ValidateIfBookingIsOnAHoliday(booking, cancellationToken))
        {
            return ValidationResult.Fail(ValidationMessages.BookingOnHoliday);
        }

        if (await ValidateIfTrainerHasTimeOffOnBookingDate(booking, cancellationToken))
        {
            return ValidationResult.Fail(ValidationMessages.BookingOutsideAvailability);
        }

        if (!BookingDurationInValidIntervals(booking, out var validationResult))
        {
            return validationResult;
        }

        if (!await ValidateIfTrainerIsWorkingOnBookingDate(booking, cancellationToken))
        {
            return ValidationResult.Fail(ValidationMessages.BookingOutsideAvailability);
        }

        if (await ValidateIfTrainerSessionsOverlapBooking(booking, excludeSessionId, cancellationToken))
        {
            return ValidationResult.Fail(ValidationMessages.BookingOverlapsExisting);
        }

        return ValidationResult.Success();
    }

    public async Task<List<AvailableSlot>> GetAvailableSlotsAsync(
        Guid trainerId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        if (trainerId == Guid.Empty)
        {
            throw new ArgumentException($"{trainerId} is an invalid value for trainer ID", nameof(trainerId));
        }

        var dateOnly = date.Date;

        var holidaysForMonth = await _holidayService.FetchHolidaysForMonth(
            dateOnly.Month, dateOnly.Year, cancellationToken: cancellationToken);

        if (holidaysForMonth.Any(holiday => holiday.Date.Date == dateOnly))
        {
            return new List<AvailableSlot>();
        }

        var trainerTimeOffsInMonth = await _timeOffService.GetAllForTrainerIdInMonthAsync(
            trainerId, dateOnly.Month, cancellationToken: cancellationToken);

        if (trainerTimeOffsInMonth.Any(timeOff => timeOff.Date.Date == dateOnly))
        {
            return new List<AvailableSlot>();
        }

        var availabilities = await _trainerAvailabilitiesService.GetAvailabilitiesForTrainerIdAsync(
            trainerId, cancellationToken: cancellationToken);
        var dailyAvailability = availabilities
            .SelectMany(a => a.DailyAvailabilities)
            .FirstOrDefault(d => d.DayOfWeek == dateOnly.DayOfWeek.ToString());

        if (dailyAvailability is null
            || dailyAvailability.IsDayOff
            || dailyAvailability.WorkingHours.Count == 0)
        {
            return new List<AvailableSlot>();
        }

        var sessionsInMonth = await _trainingSessionsService.GetTrainingSessionsForTrainerIdInMonthAsync(
            trainerId, dateOnly.Month, cancellationToken: cancellationToken);

        // Expanding each active session by the buffer on both sides turns the asymmetric
        // overlap condition in ValidateIfTrainerSessionsOverlapBooking into plain interval
        // subtraction: any candidate slot that doesn't intersect one of these expanded
        // intervals is guaranteed safe under that same condition.
        var busyIntervals = sessionsInMonth
            .Where(x => ActiveTrainingSessionStatuses.Contains(x.Status) && x.StartTime.Date == dateOnly)
            .Select(x => (
                Start: x.StartTime.AddMinutes(-BufferBetweenTrainingSessions),
                End: x.EndTime.AddMinutes(BufferBetweenTrainingSessions)))
            .OrderBy(x => x.Start)
            .ToList();

        var now = DateTime.UtcNow;
        var minDurationMinutes = ValidDurationsMinutes.Min();
        var slots = new List<AvailableSlot>();

        foreach (var workingHours in dailyAvailability.WorkingHours)
        {
            var rangeStart = dateOnly.Add(workingHours.StartTime.ToTimeSpan());
            var rangeEnd = dateOnly.Add(workingHours.EndTime.ToTimeSpan());

            foreach (var freeInterval in SubtractBusyIntervals(rangeStart, rangeEnd, busyIntervals))
            {
                var candidate = freeInterval.Start;

                while (candidate.AddMinutes(minDurationMinutes) <= freeInterval.End)
                {
                    if (candidate >= now)
                    {
                        var availableDurations = ValidDurationsMinutes
                            .Where(d => candidate.AddMinutes(d) <= freeInterval.End)
                            .ToList();

                        if (availableDurations.Count > 0)
                        {
                            slots.Add(new AvailableSlot { StartTime = candidate, AvailableDurationsMinutes = availableDurations });
                        }
                    }

                    candidate = candidate.AddMinutes(SlotGranularityMinutes);
                }
            }
        }

        return slots.OrderBy(x => x.StartTime).ToList();
    }

    // A session only occupies the trainer's time while it's actually active - Requested (not
    // yet declined) or Booked (accepted). Cancelled/Completed/NoShow/Reschedule sessions must
    // NOT block a new booking in that slot, otherwise e.g. declining a request would leave the
    // slot permanently stuck instead of freeing it back up.
    private static readonly int[] ActiveTrainingSessionStatuses =
    {
        (int)TrainingSessionStatus.Requested,
        (int)TrainingSessionStatus.Booked
    };

    private async Task<bool> ValidateIfTrainerSessionsOverlapBooking(
        InsertTrainingSession booking,
        Guid? excludeSessionId,
        CancellationToken cancellationToken)
    {
        var trainerBookingsInMonth = (await _trainingSessionsService
            .GetTrainingSessionsForTrainerIdInMonthAsync(
                booking.TrainerId,
                booking.StartTime.Month,
                cancellationToken: cancellationToken))
            .Where(x => ActiveTrainingSessionStatuses.Contains(x.Status))
            .Where(x => excludeSessionId is null || x.Id != excludeSessionId)
            .ToList();

        var result = trainerBookingsInMonth
            .Any(x => booking.StartTime < x.EndTime.AddMinutes(BufferBetweenTrainingSessions)
                && x.StartTime < booking.EndTime.AddMinutes(BufferBetweenTrainingSessions));

        return result;
    }

    private async Task<bool> ValidateIfTrainerIsWorkingOnBookingDate(
        InsertTrainingSession booking,
        CancellationToken cancellationToken)
    {
        var isTrainerWorkingOnDate = await _trainerAvailabilitiesService.IsTrainerWorkingOnDateAsync(
            booking.TrainerId,
            booking.StartTime,
            booking.EndTime,
            cancellationToken: cancellationToken);

        return isTrainerWorkingOnDate;
    }

    private bool BookingDurationInValidIntervals(InsertTrainingSession booking, out ValidationResult validationResult)
    {
        var duration = TimeSpan.FromSeconds(Math.Floor((booking.EndTime - booking.StartTime).TotalSeconds));
        var validDurations = ValidDurationsMinutes.Select(d => TimeSpan.FromMinutes(d));

        if (validDurations.Contains(duration))
        {
            validationResult = ValidationResult.Success();
            return true;
        }
        
        if (duration < TimeSpan.FromMinutes(30))
        {
            validationResult = ValidationResult.Fail(ValidationMessages.BookingTooShort);
            return false;
        }

        if (duration > TimeSpan.FromMinutes(90))
        {
            validationResult = ValidationResult.Fail(ValidationMessages.BookingTooLong);
            return false;
        }

        validationResult = ValidationResult.Fail(ValidationMessages.BookingIntervals);
        return false;
    }

    private async Task<bool> ValidateIfTrainerHasTimeOffOnBookingDate(
        InsertTrainingSession booking, 
        CancellationToken cancellationToken)
    {
        var trainerTimeOffsInBookingMonth = (await _timeOffService
            .GetAllForTrainerIdInMonthAsync(
                booking.TrainerId, 
                booking.StartTime.Month, 
                cancellationToken: cancellationToken))
            .ToList();

        var result = trainerTimeOffsInBookingMonth.Any(x => x.Date.Date == booking.StartTime.Date);

        return result;
    }

    private async Task<bool> ValidateIfBookingIsOnAHoliday(
        InsertTrainingSession booking, 
        CancellationToken cancellationToken)
    {
        var holidaysForMonth = await _holidayService.FetchHolidaysForMonth(
            booking.StartTime.Month,
            booking.StartTime.Year, 
            cancellationToken: cancellationToken);

        var result = holidaysForMonth.Any(holiday => holiday.Date.Date == booking.StartTime.Date);

        return result;
    }

    // Standard interval subtraction: walks the sorted, buffer-expanded busy intervals once,
    // emitting the gaps between them (and before/after) that fall inside [rangeStart, rangeEnd).
    private static List<(DateTime Start, DateTime End)> SubtractBusyIntervals(
        DateTime rangeStart,
        DateTime rangeEnd,
        List<(DateTime Start, DateTime End)> busyIntervals)
    {
        var freeIntervals = new List<(DateTime Start, DateTime End)>();
        var cursor = rangeStart;

        foreach (var busy in busyIntervals.Where(b => b.End > rangeStart && b.Start < rangeEnd))
        {
            var busyStart = busy.Start < rangeStart ? rangeStart : busy.Start;
            var busyEnd = busy.End > rangeEnd ? rangeEnd : busy.End;

            if (busyStart > cursor)
            {
                freeIntervals.Add((cursor, busyStart));
            }

            if (busyEnd > cursor)
            {
                cursor = busyEnd;
            }
        }

        if (cursor < rangeEnd)
        {
            freeIntervals.Add((cursor, rangeEnd));
        }

        return freeIntervals;
    }
}