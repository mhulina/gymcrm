using GymCRM.MembershipAPI.Infrastructure.Entities;
using GymCRM.MembershipAPI.Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.MembershipAPI.Infrastructure.Implementation
{
	public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity
	{
		protected DbContext _context { get; set; }
		protected DbSet<TEntity> _dbSet { get; set; }

		public GenericRepository(DbContext context)
		{
			_context = context;
			_dbSet = context.Set<TEntity>();
		}

		public bool BulkDelete(IEnumerable<TEntity> entities)
		{
			var result = new List<TEntity>();
			var entitiesHaveIDs = entities.All(x => x.Id > 0);

			result = !entitiesHaveIDs
				? _dbSet
					.AsNoTracking()
					.Where(x => entities.Select(y => y.Guid).Contains(x.Guid))
					.ToList()
				: entities.ToList();

			if (result.Count > 0)
			{
				_dbSet.RemoveRange(result);

				return true;
			}

			return false;
		}

		public void BulkInsert(IEnumerable<TEntity> entities)
		{
			foreach (var entity in entities)
			{
				entity.Guid = Guid.NewGuid();
			}

			_dbSet.AddRange(entities);
		}

		public bool Delete(TEntity entity)
		{
			if (entity.Guid != Guid.Empty)
			{
				var result = _dbSet.AsNoTracking().FirstOrDefault(x => x.Guid == entity.Guid);

				if (result != null)
				{
					_dbSet.Remove(result);

					return true;
				}
			}

			return false;
		}

		public IEnumerable<TEntity> FetchAll()
		{
			var result = _dbSet.AsNoTracking();

			return result;
		}

		public IEnumerable<TEntity> FetchByCondition(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression)
		{
			var result = _dbSet.Where(expression).AsNoTracking();

			return result;
		}

		public void Insert(TEntity entity)
		{
			entity.Guid = Guid.NewGuid();

			_dbSet.Add(entity);
		}

		public void Update(TEntity entity)
		{
			if (entity.Guid != Guid.Empty)
			{
				var entityID = _dbSet.AsNoTracking().FirstOrDefault(x => x.Guid == entity.Guid)?.Id;

				if (entityID > 0)
				{
					entity.Id = entityID.Value;
				}
			}

			_dbSet.Update(entity);
		}
	}
}
