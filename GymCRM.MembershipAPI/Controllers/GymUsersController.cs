using FluentResults;
using GymCRM.MembershipAPI.Models.DTOs;
using GymCRM.MembershipAPI.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace GymCRM.MembershipAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class GymUsersController : ControllerBase
	{
		private readonly IGymUsersService _gymUsersService;
		private ResponseDto _responseDto;

		public GymUsersController(IGymUsersService gymUsersService)
		{
			_gymUsersService = gymUsersService;
			_responseDto = new ResponseDto();
		}

		[HttpGet]
		public Result<List<UserDto>> GetAllUsers()
		{
			try
			{
				var result = _gymUsersService.GetAllUsers();

				return Result.Ok(result);
			}
			catch (Exception ex)
			{
				return Result.Fail(ex.Message);
			}
		}

		[HttpGet("{guid}")]
		public Result<UserDto> GetUserById(Guid guid)
		{
			try
			{
				var result = _gymUsersService.GetByGuid(guid);

				return Result.OkIf(result != null, "User does not exist");
			}
			catch (Exception ex)
			{
				return Result.Fail(ex.Message);
			}
		}
	}
}
