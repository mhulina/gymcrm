using System.Security.Claims;
using Asp.Versioning;
using GymCRM.IdentityAPI.Models;
using GymCRM.IdentityAPI.Models.DTOs;
using GymCRM.IdentityAPI.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace GymCRM.IdentityAPI.Controllers
{
	// [EnableCors("AllowAny")]
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
		public async Task<ActionResult<Member>> GetMe()
		{
			try
			{
				var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				
				if (string.IsNullOrEmpty(userIdClaim)
				    || !Guid.TryParse(userIdClaim, out var userId))
				{
					return new UnauthorizedObjectResult("Invalid token claims");
				}
				
				var result = await _membersService.GetUserByGuidAsync(userId);

				if (result != null)
				{
					return new OkObjectResult(result);
				}
				
				return new NotFoundResult();
			}
			catch (Exception)
			{
				return new StatusCodeResult(StatusCodes.Status500InternalServerError);
			}
		}
		
		/// <summary>
		/// Retrieves a list of all users.
		/// </summary>
		/// <returns>
		/// An <see cref="OkObjectResult"/> containing a list of users,
		/// or a <see cref="StatusCodeResult"/> with status 500 if an error occurs.
		/// </returns>
		/// <response code="200">Returns the list of all users.</response>
		/// <response code="500">Indicates an unexpected error occurred.</response>
		[HttpGet]
		public async Task<ActionResult<List<Member>>> GetAllUsers()
		{
			try
			{
				var result = await _membersService.GetAllUsersAsync();

				return new OkObjectResult(result);
			}
			catch (Exception)
			{
				return new StatusCodeResult(StatusCodes.Status500InternalServerError);
			}
		}

		/// <summary>
		/// Retrieves a user by their unique GUID identifier.
		/// </summary>
		/// <param name="guid">The GUID of the user to retrieve.</param>
		/// <returns>
		/// An <see cref="OkObjectResult"/> containing the user if found,
		/// a <see cref="NotFoundResult"/> if no user is found,
		/// or a <see cref="StatusCodeResult"/> with status 500 if an error occurs.
		/// </returns>
		/// <response code="200">Returns the user with the specified GUID.</response>
		/// <response code="404">User with the specified GUID was not found.</response>
		/// <response code="500">Indicates an unexpected error occurred.</response>
		[HttpGet("{guid:guid}")]
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
				return new StatusCodeResult(StatusCodes.Status500InternalServerError);
			}
		}

		/// <summary>
		/// Retrieves a user by their email address.
		/// </summary>
		/// <param name="email">The email address of the user to retrieve.</param>
		/// <returns>
		/// An <see cref="OkObjectResult"/> containing the user if found,
		/// a <see cref="NotFoundResult"/> if no user is found,
		/// or a <see cref="StatusCodeResult"/> with status 500 if an error occurs.
		/// </returns>
		/// <response code="200">Returns the user with the specified email.</response>
		/// <response code="404">User with the specified email was not found.</response>
		/// <response code="500">Indicates an unexpected error occurred.</response>
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
				return new StatusCodeResult(StatusCodes.Status500InternalServerError);
			}
		}

		/// <summary>
		/// Updates the information of an existing member.
		/// </summary>
		/// <param name="newMember">The member object containing updated information.</param>
		/// <returns>
		/// An <see cref="OkObjectResult"/> indicating success,
		/// a <see cref="BadRequestResult"/> if the update failed,
		/// or a <see cref="NotFoundObjectResult"/> if the member was not found.
		/// </returns>
		/// <response code="200">Indicates that the member was successfully updated.</response>
		/// <response code="400">Indicates the update operation failed.</response>
		/// <response code="404">Indicates the member to update was not found.</response>
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
