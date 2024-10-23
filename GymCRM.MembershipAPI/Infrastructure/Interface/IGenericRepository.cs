using System.Linq.Expressions;
using GymCRM.MembershipAPI.Infrastructure.Entities;

namespace GymCRM.MembershipAPI.Infrastructure.Interface
{
	public interface IGenericRepository<TEntity> where TEntity : BaseEntity
	{
		IEnumerable<TEntity> FetchAll();
		IEnumerable<TEntity> FetchByCondition(Expression<Func<TEntity, bool>> expression);
		void Insert(TEntity entity);
		void BulkInsert(IEnumerable<TEntity> entities);
		void Update(TEntity entity);
		bool Delete(TEntity entity);
		bool BulkDelete(IEnumerable<TEntity> entities);
	}
}
