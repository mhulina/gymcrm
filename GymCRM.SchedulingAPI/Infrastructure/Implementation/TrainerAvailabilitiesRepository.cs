using System.Linq.Expressions;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.SchedulingAPI.Infrastructure.Implementation;

public class TrainerAvailabilitiesRepository : GenericRepository<TrainerAvailability>, ITrainerAvailabilitiesRepository
{
    public TrainerAvailabilitiesRepository(SchedulingDbContext context) : base(context) {}
    
    public async Task<IEnumerable<TrainerAvailability>> FetchAllAsync(CancellationToken cancellationToken)
    {
        var result = await _dbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken: cancellationToken);
        
        return result;
    }

    public async Task<IEnumerable<TrainerAvailability>> FetchByConditionAsync(
        Expression<Func<TrainerAvailability, bool>> expression, 
        CancellationToken cancellationToken)
    {
        var result = await _dbSet
            .AsNoTracking()
            .Where(expression)
            .ToListAsync(cancellationToken: cancellationToken);

        return result;
    }
}