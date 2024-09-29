using FluentResults;
using GymCRM.UsersAPI.Models.DTOs;
using GymCRM.UsersAPI.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace GymCRM.UsersAPI.Controllers
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
			var result = _gymUsersService.GetAllUsers();

			return result;
		}
	}
}
