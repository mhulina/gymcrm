using System.Linq.Expressions;
using GymCRM.MembershipAPI.Models.Entities;

namespace GymCRM.MembershipAPI.Models.Interface;

public interface IAccountsRepository : IDisposable
{
    /// <summary>
    /// Retrieves all <see cref="Account"/> entities from the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing an enumerable collection of <see cref="Account"/> entities.
    /// </returns>
    Task<IEnumerable<Account>> FetchAllAccountsAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Retrieves <see cref="Account"/> entities that satisfy the specified filter condition asynchronously.
    /// Includes related <see cref="Member"/> entities.
    /// </summary>
    /// <param name="expression">A LINQ expression used to filter the accounts.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing an enumerable collection of <see cref="Account"/> entities that match the filter.
    /// </returns>
    Task<IEnumerable<Account>> FetchByConditionAsync(
        Expression<Func<Account, bool>> expression,
        CancellationToken cancellationToken);
    /// <summary>
    /// Inserts a new <see cref="Account"/> entity into the database context.
    /// </summary>
    /// <param name="entity">The <see cref="Account"/> entity to insert.</param>
    void Insert(Account entity);
    /// <summary>
    /// Deletes a <see cref="Account"/> entity from the database context.
    /// </summary>
    /// <param name="entity">The <see cref="Account"/> entity to delete.</param>
    void Delete(Account entity);
    /// <summary>
    /// Updates an existing <see cref="Account"/> entity in the database context.
    /// </summary>
    /// <param name="entity">The <see cref="Account"/> entity to update.</param>
    void Update(Account entity);
}