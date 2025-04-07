using Asp.Versioning;
using GymCRM.MembershipAPI.Infrastructure;
using GymCRM.MembershipAPI.Models.DTOs;
using GymCRM.MembershipAPI.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace GymCRM.MembershipAPI.Controllers
{
	[EnableCors("AllowAny")]
	[ApiVersion("1.0")]
	[Route("api/v{version:apiVersion}/[controller]/[action]")]
	[Authorize]
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
		public ActionResult<List<MemberDto>> GetAllUsers()
		{
			try
			{
				var result = _membersService.GetAllUsers();

				return new OkObjectResult(result);
			}
			catch (Exception)
			{
				return new StatusCodeResult(500);
			}
		}

		[HttpGet("{guid}")]
		public ActionResult<MemberDto> GetUserByGuid(Guid guid)
		{
			try
			{
				var result = _membersService.GetByGuid(guid);

				if (result != null)
				{
					return new OkObjectResult(result);
				}
				
				return new NotFoundResult();
			}
			catch (Exception)
			{
				return new StatusCodeResult(500);
			}
		}

		[HttpGet("{email}")]
		public ActionResult<MemberDto> GetUserByEmail(string email)
		{
			try
			{
				var result = _membersService.GetByEmail(email);

				if (result != null)
				{
					return new OkObjectResult(result);
				}
				
				return new NotFoundResult();
			}
			catch (Exception)
			{
				return new StatusCodeResult(500);
			}
		}

		[HttpPut]
		public ActionResult<bool> UpdateMember([FromBody]MemberDto newMemberDto)
		{
			try
			{
				var result = _membersService.UpdateMember(newMemberDto);

				if (result)
				{
					return new OkObjectResult(result);
				}
				
				return new BadRequestResult();
			}
			catch (MemberNotFoundException)
			{
				return new NotFoundObjectResult(newMemberDto.AccountGuid);
			}
		}

		[HttpPost]
		public ActionResult<bool> AddMember([FromBody] MemberDto newMemberDto)
		{
			try
			{
				var result = _membersService.InsertMember(newMemberDto);
				
				if (result)
				{
					return new OkObjectResult(result);
				}
				
				return new BadRequestResult();
			}
			catch (Exception)
			{
				return new StatusCodeResult(500);
			}
		}
	}
}
