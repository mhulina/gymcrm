using System.Linq.Expressions;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.SchedulingAPI.Infrastructure.Implementation;

public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
{
    protected readonly SchedulingDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public GenericRepository(SchedulingDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }
    
    public async Task<IEnumerable<TEntity>> FetchAllAsync(CancellationToken cancellationToken)
    {
        var result = await _dbSet.AsNoTracking().ToListAsync(cancellationToken: cancellationToken);
        
        return result;
    }

    public async Task<IEnumerable<TEntity>> FetchByConditionAsync(
        Expression<Func<TEntity, bool>> expression, 
        CancellationToken cancellationToken)
    {
        var result = await _dbSet
            .AsNoTracking()
            .Where(expression)
            .ToListAsync(cancellationToken: cancellationToken);

        return result;
    }

    public void Remove(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    public void Add(TEntity entity)
    {
        _dbSet.Add(entity);
    }

    public void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }
}