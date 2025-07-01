using GymCRM.MembershipAPI.Infrastructure.Entities;
using GymCRM.MembershipAPI.Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;
using ILogger = Serilog.ILogger;

namespace GymCRM.MembershipAPI.Infrastructure.Implementation
{
	public class MembersRepository : IMembersRepository
	{
		private readonly AppDbContext _context;
		private readonly ILogger _logger;

		public MembersRepository(AppDbContext context, ILogger logger)
		{
			_context = context;
			_logger = logger;
		}
		
		public async Task<IEnumerable<Member>> FetchAll(CancellationToken cancellationToken)
		{
			var result = await _context.Members
				.AsNoTracking()
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
