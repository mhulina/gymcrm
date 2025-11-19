using System.Linq.Expressions;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.SchedulingAPI.Infrastructure.Implementation;

public class TrainerDailyAvailabilitiesesRepository : GenericRepository<TrainerDailyAvailability>, ITrainerDailyAvailabilitiesRepository
{
    public TrainerDailyAvailabilitiesesRepository(SchedulingDbContext context) : base(context) { }
    
    public async Task<IEnumerable<TrainerDailyAvailability>> FetchAllAsync(CancellationToken cancellationToken)
    {
        var result = await _dbSet
            .AsNoTracking()
            .Include(x => x.WorkingHours)
            .ToListAsync(cancellationToken: cancellationToken);
        
        return result;
    }

    public async Task<IEnumerable<TrainerDailyAvailability>> FetchByConditionAsync(
        Expression<Func<TrainerDailyAvailability, bool>> expression, 
        CancellationToken cancellationToken)
    {
        var result = await _dbSet
            .AsNoTracking()
            .Where(expression)
            .Include(x => x.WorkingHours)
            .ToListAsync(cancellationToken: cancellationToken);

        return result;
    }
}