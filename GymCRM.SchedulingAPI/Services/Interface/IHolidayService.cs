using GymCRM.SchedulingAPI.Models.DTOs;

namespace GymCRM.SchedulingAPI.Services.Interface;

public interface IHolidayService
{
    Task<List<Holiday>> FetchHolidaysForMonth(int month, int year, CancellationToken cancellationToken = default);
    Task<List<Holiday>> FetchAllHolidays(CancellationToken cancellationToken = default);
}