using GymCRM.SchedulingAPI.Models.Entities;

namespace GymCRM.SchedulingAPI.Infrastructure.Interface;

public interface IHolidayRepository
{
    Task<List<Holiday>> GetAllAsync(CancellationToken cancellationToken);
    Task<Holiday> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Holiday> GetByDateAsync(DateTime date, CancellationToken cancellationToken);
    Task<List<Holiday>> GetByMonthAsync(int month, int year, CancellationToken cancellationToken);
    Task<List<Holiday>> GetByYearAsync(DateTime date, CancellationToken cancellationToken);
    void Add(Holiday holiday);
    void Update(Holiday holiday);
    void Delete(Holiday holiday);
}