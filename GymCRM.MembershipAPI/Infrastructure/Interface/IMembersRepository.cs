using System.Linq.Expressions;
using GymCRM.MembershipAPI.Infrastructure.Entities;

namespace GymCRM.MembershipAPI.Infrastructure.Interface
{
	public interface IMembersRepository : IDisposable
	{
		IEnumerable<Member> FetchAll();
		IEnumerable<Member> FetchByCondition(Expression<Func<Member, bool>> expression);
		void Insert(Member entity);
		bool Save();
		void BulkInsert(IEnumerable<Member> entities);
		bool BulkDelete(IEnumerable<Member> entities);
		bool Delete(Member entity);
		void Update(Member entity);
	}
}
