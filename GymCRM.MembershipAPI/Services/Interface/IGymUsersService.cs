using GymCRM.MembershipAPI.Models.DTOs;

namespace GymCRM.MembershipAPI.Services.Interface
{
	public interface IGymUsersService
	{
		List<UserDto> GetAllUsers();
		UserDto GetByGuid(Guid guid);
	}
}
