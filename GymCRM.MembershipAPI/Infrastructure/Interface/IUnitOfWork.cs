namespace GymCRM.MembershipAPI.Models.Interface;

public interface IUnitOfWork
{
    /// <summary>
    /// Persists all changes made in the current database context to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing true if one or more entities were persisted; otherwise, false.
    /// </returns>
    Task<bool> SaveAsync(CancellationToken cancellationToken);
}