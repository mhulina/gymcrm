using System.Linq.Expressions;
using GymCRM.IdentityAPI.Models.Entities;

namespace GymCRM.IdentityAPI.Models.Interface
{
	public interface IMembersRepository : IDisposable
	{
		/// <summary>
		/// Retrieves all <see cref="Member"/> entities from the database.
		/// </summary>
		/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task representing the asynchronous operation, containing an enumerable collection of <see cref="Member"/> entities.
		/// </returns>
		Task<IEnumerable<Member>> FetchAll(CancellationToken cancellationToken);
		/// <summary>
		/// Retrieves <see cref="Member"/> entities that satisfy the specified filter condition.
		/// </summary>
		/// <param name="expression">A LINQ expression used to filter the members.</param>
		/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
		/// <returns>
		/// A task representing the asynchronous operation, containing an enumerable collection of <see cref="Member"/> entities that match the filter.
		/// </returns>
		Task<IEnumerable<Member>> FetchByCondition(Expression<Func<Member, bool>> expression, CancellationToken cancellationToken);
		/// <summary>
		/// Inserts a new <see cref="Member"/> entity into the database context.
		/// </summary>
		/// <param name="entity">The <see cref="Member"/> entity to insert.</param>
		void Insert(Member entity);
		/// <summary>
		/// Inserts multiple <see cref="Member"/> entities into the database context in bulk.
		/// </summary>
		/// <param name="entities">The collection of <see cref="Member"/> entities to insert.</param>
		void BulkInsert(IEnumerable<Member> entities);
		/// <summary>
		/// Deletes multiple <see cref="Member"/> entities from the database context in bulk.
		/// </summary>
		/// <param name="entities">The collection of <see cref="Member"/> entities to delete.</param>
		void BulkDelete(IEnumerable<Member> entities);
		/// <summary>
		/// Deletes a <see cref="Member"/> entity from the database context.
		/// </summary>
		/// <param name="entity">The <see cref="Member"/> entity to delete.</param>
		void Delete(Member entity);
		/// <summary>
		/// Updates an existing <see cref="Member"/> entity in the database context.
		/// </summary>
		/// <param name="entity">The <see cref="Member"/> entity to update.</param>
		void Update(Member entity);
	}
}
