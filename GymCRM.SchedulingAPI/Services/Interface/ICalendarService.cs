using GymCRM.SchedulingAPI.Models.DTOs;

namespace GymCRM.SchedulingAPI.Services.Interface;

public interface ICalendarService
{
    public Task<GymTrainerCalendarDto> GetGymTrainerCalendarForMonthAsync(
        Guid trainerId, 
        int month, 
        int year, 
        CancellationToken token = default);
}