using FluentResults;
using GymCRM.MembershipAPI.Infrastructure;
using GymCRM.MembershipAPI.Models.DTOs;
using GymCRM.MembershipAPI.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace GymCRM.MembershipAPI.Controllers
{
	[Route("api/[controller]/[action]")]
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
		public Result<MemberDto> GetUserByGuid(Guid guid)
		{
			try
			{
				var result = _membersService.GetByGuid(guid);

				return Result
					.OkIf(result != null, "User does not exist")
					.ToResult(result);
			}
			catch (Exception ex)
			{
				return Result.Fail(ex.Message);
			}
		}

		[HttpGet("{email}")]
		public Result<MemberDto> GetUserByEmail(string email)
		{
			try
			{
				var result = _membersService.GetByEmail(email);

				return Result
					.OkIf(result != null, "User does not exist")
					.ToResult(result);
			}
			catch (Exception ex)
			{
				return Result.Fail(ex.Message);
			}
		}

		[HttpPut]
		public Result<bool> UpdateMember([FromBody]MemberDto newMemberDto)
		{
			try
			{
				var result = _membersService.UpdateMember(newMemberDto);

				return Result
					.OkIf(result, "Error during updating member data")
					.ToResult(result);
			}
			catch (MemberNotFoundException ex)
			{
				return Result.Fail(ex.Message);
			}
		}

		[HttpPost]
		public Result<bool> AddMember([FromBody] MemberDto newMemberDto)
		{
			try
			{
				var result = _membersService.InsertMember(newMemberDto);
				
				return Result
					.OkIf(result, "Error during adding member data")
					.ToResult(result);
			}
			catch (Exception ex)
			{
				return Result.Fail(ex.Message);
			}
		}
	}
}
