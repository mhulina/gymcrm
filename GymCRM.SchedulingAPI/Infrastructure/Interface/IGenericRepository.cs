using System.Linq.Expressions;

namespace GymCRM.SchedulingAPI.Infrastructure.Interface;

public interface IGenericRepository<TEntity> where TEntity : class
{
    Task<IEnumerable<TEntity>> FetchAllAsync(CancellationToken cancellationToken);
    Task<IEnumerable<TEntity>> FetchByConditionAsync(
        Expression<Func<TEntity, bool>> expression,
        CancellationToken cancellationToken);
    void Remove(TEntity entity);
    void Add(TEntity entity);
    void AddRange(IEnumerable<TEntity> entities);
    void Update(TEntity entity);
}