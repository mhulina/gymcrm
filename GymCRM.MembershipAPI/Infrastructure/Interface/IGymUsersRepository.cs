using GymCRM.MembershipAPI.Infrastructure.Entities;

namespace GymCRM.MembershipAPI.Infrastructure.Interface
{
	public interface IGymUsersRepository : IDisposable
	{
		IGenericRepository<User> GymUsers { get; }
		bool Save();
	}
}
