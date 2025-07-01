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

		public MembersController(IMembersService membersService)
		{
			_membersService = membersService;
		}

		[HttpGet]
		public ActionResult<List<Member>> GetAllUsers()
		{
			try
			{
				var result = _membersService.GetAllUsersAsync();

				return new OkObjectResult(result);
			}
			catch (Exception)
			{
				return new StatusCodeResult(500);
			}
		}

		[HttpGet("{guid}")]
		public async Task<ActionResult<Member>> GetUserByGuid(Guid guid)
		{
			try
			{
				var result = await _membersService.GetUserByGuidAsync(guid);

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
		public async Task<ActionResult<Member>> GetUserByEmail(string email)
		{
			try
			{
				var result = await _membersService.GetUserByEmailAsync(email);

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
		public async Task<ActionResult<bool>> UpdateMember([FromBody]Member newMember)
		{
			try
			{
				var result = await _membersService.UpdateMemberAsync(newMember);

				if (result)
				{
					return new OkObjectResult(result);
				}
				
				return new BadRequestResult();
			}
			catch (MemberNotFoundException)
			{
				return new NotFoundObjectResult(newMember.AccountGuid);
			}
		}
	}
}
