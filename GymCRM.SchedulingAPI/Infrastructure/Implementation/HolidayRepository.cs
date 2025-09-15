using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.SchedulingAPI.Infrastructure.Implementation;

public class HolidayRepository : IHolidayRepository
{
    private readonly SchedulingDbContext _context;

    public HolidayRepository(SchedulingDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Holiday>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await _context.Holidays.ToListAsync(cancellationToken: cancellationToken);
        
        return result;
    }

    public async Task<Holiday> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _context.Holidays
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        
        return result;
    }

    public async Task<Holiday> GetByDateAsync(DateTime date, CancellationToken cancellationToken)
    {
        var result = await _context.Holidays
            .FirstOrDefaultAsync(x => x.Date == date.Date, cancellationToken: cancellationToken);
        
        return result;
    }

    public async Task<List<Holiday>> GetByMonthAsync(int month, int year, CancellationToken cancellationToken)
    {
        var result = await _context.Holidays
            .Where(x => x.Date.Month == month 
                && x.Date.Year == year)
            .ToListAsync(cancellationToken: cancellationToken);
        
        return result;
    }

    public async Task<List<Holiday>> GetByYearAsync(DateTime date, CancellationToken cancellationToken)
    {
        var result = await _context.Holidays
            .Where(x => x.Year == date.Year)
            .ToListAsync(cancellationToken: cancellationToken);
        
        return result;
    }

    public void Add(Holiday holiday)
    {
        _context.Holidays.Add(holiday);
    }

    public void Update(Holiday holiday)
    {
        _context.Holidays.Update(holiday);
    }

    public void Delete(Holiday holiday)
    {
        _context.Holidays.Remove(holiday);
    }
}