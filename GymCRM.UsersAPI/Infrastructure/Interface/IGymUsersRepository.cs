using GymCRM.UsersAPI.Infrastructure.Entities;

namespace GymCRM.UsersAPI.Infrastructure.Interface
{
	public interface IGymUsersRepository : IDisposable
	{
		IGenericRepository<User> GymUsers { get; }
		bool Save();
	}
}
