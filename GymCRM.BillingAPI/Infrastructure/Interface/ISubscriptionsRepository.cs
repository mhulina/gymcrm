using System.Linq.Expressions;
using GymCRM.BillingAPI.Models.Entities;

namespace GymCRM.BillingAPI.Infrastructure.Interface;

public interface ISubscriptionsRepository : IDisposable
{
    /// <summary>
    /// Retrieves <see cref="Subscription"/> entities that satisfy the specified filter condition
    /// asynchronously, including their <see cref="Payment"/> history.
    /// </summary>
    Task<IEnumerable<Subscription>> FetchByConditionAsync(
        Expression<Func<Subscription, bool>> expression,
        CancellationToken cancellationToken);
    /// <summary>
    /// Inserts a new <see cref="Subscription"/> entity into the database context.
    /// </summary>
    void Insert(Subscription entity);
    /// <summary>
    /// Updates an existing <see cref="Subscription"/> entity in the database context.
    /// </summary>
    void Update(Subscription entity);
}
