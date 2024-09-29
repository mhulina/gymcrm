using GymCRM.UsersAPI.Models.DTOs;

namespace GymCRM.UsersAPI.Services.Interface
{
    public interface IGymUsersService
    {
		List<UserDto> GetAllUsers();

	}
}
