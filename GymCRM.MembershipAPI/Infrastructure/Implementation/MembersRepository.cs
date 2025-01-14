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

		public bool Save()
		{
			try
			{
				_context.Database.BeginTransaction();
				var result = _context.SaveChanges();
				_context.Database.CommitTransaction();

				return result > 0;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);
				_context.Database.RollbackTransaction();
				throw;
			}
		}
		
		public IEnumerable<Member> FetchAll()
		{
			var result = _context.Members.AsNoTracking();

			return result;
		}

		public IEnumerable<Member> FetchByCondition(System.Linq.Expressions.Expression<Func<Member, bool>> expression)
		{
			var result = _context.Members.Where(expression).AsNoTracking();

			return result;
		}

		public void Insert(Member entity)
		{
			_context.Members.Add(entity);
		}
		
		public void Update(Member entity)
		{
			if (entity.AccountGuid != Guid.Empty)
			{
				var entityID = _context.Members
					.AsNoTracking()
					.FirstOrDefault(x => x.AccountGuid == entity.AccountGuid)?.Id;

				if (entityID > 0)
				{
					entity.Id = entityID.Value;
				}
			}

			_context.Members.Update(entity);
		}
		
		public bool Delete(Member entity)
		{
			if (entity.AccountGuid != Guid.Empty)
			{
				var result = _context.Members
					.AsNoTracking()
					.FirstOrDefault(x => x.AccountGuid == entity.AccountGuid);

				if (result != null)
				{
					_context.Members.Remove(result);

					return true;
				}
			}

			return false;
		}
		
		public bool BulkDelete(IEnumerable<Member> entities)
		{
			var result = new List<Member>();
			var entitiesHaveIDs = entities.All(x => x.Id > 0);

			result = !entitiesHaveIDs
				? _context.Members
					.AsNoTracking()
					.Where(x => entities.Select(y => y.AccountGuid).Contains(x.AccountGuid))
					.ToList()
				: entities.ToList();

			if (result.Count > 0)
			{
				_context.Members.RemoveRange(result);

				return true;
			}

			return false;
		}
		
		public void BulkInsert(IEnumerable<Member> entities)
		{
			foreach (var entity in entities)
			{
				entity.AccountGuid = Guid.NewGuid();
			}

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
