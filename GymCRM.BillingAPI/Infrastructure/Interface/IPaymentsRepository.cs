using System.Linq.Expressions;
using GymCRM.BillingAPI.Models.Entities;

namespace GymCRM.BillingAPI.Infrastructure.Interface;

public interface IPaymentsRepository : IDisposable
{
    /// <summary>
    /// Retrieves <see cref="Payment"/> entities that satisfy the specified filter condition asynchronously.
    /// </summary>
    Task<IEnumerable<Payment>> FetchByConditionAsync(
        Expression<Func<Payment, bool>> expression,
        CancellationToken cancellationToken);
    /// <summary>
    /// Inserts a new <see cref="Payment"/> entity into the database context.
    /// </summary>
    void Insert(Payment entity);
    /// <summary>
    /// Updates an existing <see cref="Payment"/> entity in the database context.
    /// </summary>
    void Update(Payment entity);
}
