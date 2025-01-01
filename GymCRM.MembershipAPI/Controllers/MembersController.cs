using FluentResults;
using GymCRM.MembershipAPI.Models.DTOs;
using GymCRM.MembershipAPI.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace GymCRM.MembershipAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class MembersController : ControllerBase
	{
		private readonly IMembersService _membersService;
		private ResponseDto _responseDto;

		public MembersController(IMembersService membersService)
		{
			_membersService = membersService;
			_responseDto = new ResponseDto();
		}

		[HttpGet]
		public Result<List<MemberDto>> GetAllUsers()
		{
			try
			{
				var result = _membersService.GetAllUsers();

				return Result.Ok(result);
			}
			catch (Exception ex)
			{
				return Result.Fail(ex.Message);
			}
		}

		[HttpGet("{guid}")]
		public Result<MemberDto> GetUserById(Guid guid)
		{
			try
			{
				var result = _membersService.GetByGuid(guid);

				return Result.OkIf(result.Guid != Guid.Empty, "User does not exist");
			}
			catch (Exception ex)
			{
				return Result.Fail(ex.Message);
			}
		}
	}
}
