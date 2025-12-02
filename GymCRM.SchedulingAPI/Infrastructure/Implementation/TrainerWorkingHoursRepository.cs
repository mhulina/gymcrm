using System.Linq.Expressions;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.SchedulingAPI.Infrastructure.Implementation;

public class TrainerWorkingHoursRepository : GenericRepository<TrainerWorkingHours>, ITrainerWorkingHoursRepository
{
    public TrainerWorkingHoursRepository(SchedulingDbContext context) : base(context) { }
    
    public async Task<IEnumerable<TrainerWorkingHours>> FetchAllAsync(CancellationToken cancellationToken)
    {
        var result = await _dbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken: cancellationToken);
        
        return result;
    }

    public async Task<IEnumerable<TrainerWorkingHours>> FetchByConditionAsync(
        Expression<Func<TrainerWorkingHours, bool>> expression, 
        CancellationToken cancellationToken)
    {
        var result = await _dbSet
            .AsNoTracking()
            .Where(expression)
            .ToListAsync(cancellationToken: cancellationToken);

        return result;
    }
}