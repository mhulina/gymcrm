using GymCRM.IdentityAPI.Infrastructure;
using GymCRM.IdentityAPI.Models.Entities;
using GymCRM.IdentityAPI.Models.Interface;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.IdentityAPI.Models.Implementation
{
	public class MembersRepository : IMembersRepository
	{
		private readonly IdentityDbContext _context;

		public MembersRepository(IdentityDbContext context)
		{
			_context = context;
		}
		
		public async Task<IEnumerable<Member>> FetchAll(CancellationToken cancellationToken)
		{
			var result = await _context.Members
				.AsNoTracking()
				.Include(x => x.Account)
				.ToListAsync(cancellationToken: cancellationToken);

			return result;
		}

		public async Task<IEnumerable<Member>> FetchByCondition(
			System.Linq.Expressions.Expression<Func<Member, bool>> expression,
			CancellationToken cancellationToken)
		{
			var result = await _context.Members
				.Where(expression)
				.AsNoTracking()
				.ToListAsync(cancellationToken: cancellationToken);

			return result;
		}

		// Cheap existence check - avoids materializing full Member rows (including the Photo
		// bytea column) just to answer a boolean question. Used on every unauthenticated page
		// load for the admin-setup gate, so this matters more than FetchByCondition's cost does.
		public Task<bool> AnyByAccountTypeAsync(int accountType, CancellationToken cancellationToken)
		{
			return _context.Members
				.AsNoTracking()
				.AnyAsync(x => x.AccountType == accountType, cancellationToken);
		}

		public void Insert(Member entity)
		{
			_context.Members.Add(entity);
		}
		
		public void Update(Member entity)
		{
			_context.Members.Update(entity);
		}
		
		public void Delete(Member entity)
		{
			_context.Members.Remove(entity);
		}
		
		public void BulkDelete(IEnumerable<Member> entities)
		{
			_context.Members.RemoveRange(entities);
		}
		
		public void BulkInsert(IEnumerable<Member> entities)
		{
			_context.Members.AddRange(entities);
		}

		private bool _disposed = false;
		protected virtual void Dispose(bool disposing)
		{
			if (!this._disposed)
			{
				if (disposing)
				{
					_context.Dispose();
				}
			}
			this._disposed = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
