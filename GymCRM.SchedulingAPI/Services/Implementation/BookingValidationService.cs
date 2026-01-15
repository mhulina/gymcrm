using GymCRM.SchedulingAPI.Constants;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Services.Interface;

namespace GymCRM.SchedulingAPI.Services.Implementation;

public class BookingValidationService : IBookingValidationService
{
    private const int BufferBetweenTrainingSessions = 15;
    
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
        CancellationToken cancellationToken = default)
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

        if (await ValidateIfTrainerSessionsOverlapBooking(booking, cancellationToken))
        {
            return ValidationResult.Fail(ValidationMessages.BookingOverlapsExisting);
        }

        return ValidationResult.Success();
    }

    private async Task<bool> ValidateIfTrainerSessionsOverlapBooking(
        InsertTrainingSession booking,
        CancellationToken cancellationToken)
    {
        var trainerBookingsInMonth = (await _trainingSessionsService
            .GetTrainingSessionsForTrainerIdInMonthAsync(
                booking.TrainerId,
                booking.StartTime.Month,
                cancellationToken: cancellationToken))
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
        var validDurations = new[]
        {
            TimeSpan.FromMinutes(30),
            TimeSpan.FromMinutes(60),
            TimeSpan.FromMinutes(90)
        };

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
}