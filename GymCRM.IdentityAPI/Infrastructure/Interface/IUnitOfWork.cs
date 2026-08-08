namespace GymCRM.IdentityAPI.Models.Interface;

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
    /// <summary>
    /// Detaches all currently tracked entities from the Entity Framework change tracker.
    /// This prevents tracking conflicts when performing subsequent update operations.
    /// </summary>
    void DetachAll();
    /// <summary>
    /// Detaches a specific entity from the Entity Framework change tracker.
    /// Use this before updating an entity to avoid "instance is already being tracked" exceptions.
    /// </summary>
    /// <typeparam name="T">The entity type to detach.</typeparam>
    /// <param name="entity">The entity instance to detach.</param>
    void Detach<T>(T entity) where T : class;
}