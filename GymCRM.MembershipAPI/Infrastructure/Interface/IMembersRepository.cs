using GymCRM.MembershipAPI.Infrastructure.Entities;

namespace GymCRM.MembershipAPI.Infrastructure.Interface
{
	public interface IMembersRepository : IDisposable
	{
		IGenericRepository<Member> Members { get; }
		bool Save();
	}
}
