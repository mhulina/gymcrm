using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Services.Interface;

namespace GymCRM.SchedulingAPI.Services.Implementation;

public class CalendarService : ICalendarService
{
    private readonly ITrainerAvailabilitiesService _trainerAvailabilitiesService;
    private readonly IHolidayService _holidayService;
    private readonly ITimeOffService _timeOffService;
    private readonly ITrainingSessionsService _trainingSessionsService;

    public CalendarService(
        ITrainerAvailabilitiesService trainerAvailabilitiesService,
        IHolidayService holidayService,
        ITimeOffService timeOffService,
        ITrainingSessionsService trainingSessionsService)
    {
        _trainerAvailabilitiesService = trainerAvailabilitiesService;
        _holidayService = holidayService;
        _timeOffService = timeOffService;
        _trainingSessionsService = trainingSessionsService;
    }
    
    public async Task<GymTrainerCalendarDto> GetGymTrainerCalendarForMonthAsync(
        Guid trainerId, 
        int month, 
        int year, 
        CancellationToken cancellationToken = default)
    {
        var availabilities = await _trainerAvailabilitiesService.GetAvailabilitiesForTrainerIdAsync(
                trainerId,
                cancellationToken: cancellationToken);
        var holidaysInMonth = await _holidayService.FetchHolidaysForMonth(month, year, cancellationToken: cancellationToken);
        var timeOffs = await _timeOffService.GetAllForTrainerIdAsync(trainerId, cancellationToken: cancellationToken);
        var trainingSessions = await _trainingSessionsService.GetTrainingSessionsForTrainerIdAsync(
            trainerId,
            cancellationToken: cancellationToken);
        
        var availabilitiesInMonth = availabilities
            .Where(x => (x.StartDate.Month == month && x.StartDate.Year == year)
                && (x.EndDate.Month == month && x.EndDate.Year == year)
                && !holidaysInMonth.Exists(y => y.Date == x.StartDate || y.Date == x.EndDate)
                && x.TrainerId == trainerId)
            .ToList();
        var timeOffsInMonth = timeOffs
            .Where(x => x.Date.Month == month 
                && x.Date.Year == year
                && x.TrainerId == trainerId)
            .ToList();
        var trainingSessionsInMonth = trainingSessions
            .Where(x => (x.StartTime.Month == month && x.StartTime.Year == year)
                && (x.EndTime.Month == month && x.EndTime.Year == year)
                && x.TrainerId == trainerId)
            .ToList();

        var trainerCalendarForMonth = new GymTrainerCalendarDto
        {
            Month = month,
            Year = year,
            TrainerId = trainerId,
            Availabilities = availabilitiesInMonth,
            TimeOffs = timeOffsInMonth,
            TrainingSessions = trainingSessionsInMonth
        };

        return trainerCalendarForMonth;
    }
}