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
			catch (Exception ex)
			{
				return new BadRequestObjectResult(ex.Message);
			}
		}

		/// <summary>
		/// Uploads or replaces a member's profile photo. Allowed for the member themselves or an Admin.
		/// </summary>
		/// <param name="accountGuid">The account whose photo is being set.</param>
		/// <param name="file">The image file (jpeg/png/webp, up to 5MB).</param>
		/// <response code="200">Photo saved successfully.</response>
		/// <response code="400">No file was provided, or the save failed.</response>
		/// <response code="401">The caller's token claims are invalid.</response>
		/// <response code="403">The caller may not change this member's photo.</response>
		/// <response code="404">The target member was not found.</response>
		/// <response code="413">The photo exceeds the maximum allowed size.</response>
		/// <response code="415">The photo's content type is not supported.</response>
		[HttpPost("{accountGuid:guid}")]
		[RequestSizeLimit(10 * 1024 * 1024)]
		public async Task<ActionResult<bool>> UploadPhoto(Guid accountGuid, [FromForm] IFormFile? file)
		{
			try
			{
				if (file is null || file.Length == 0)
				{
					return new BadRequestObjectResult("No photo file was provided");
				}

				var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

				if (string.IsNullOrEmpty(userIdClaim)
				    || !Guid.TryParse(userIdClaim, out var callerId))
				{
					return new UnauthorizedObjectResult("Invalid token claims");
				}

				await using var stream = file.OpenReadStream();
				using var memoryStream = new MemoryStream();
				await stream.CopyToAsync(memoryStream);

				var result = await _membersService.UploadMemberPhotoAsync(accountGuid, callerId, memoryStream.ToArray(), file.ContentType);

				return result ? new OkObjectResult(true) : new BadRequestObjectResult("Failed to save photo");
			}
			catch (MemberPhotoAccessDeniedException ex)
			{
				return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
			}
			catch (MemberNotFoundException ex)
			{
				return new NotFoundObjectResult(ex.Message);
			}
			catch (InvalidPhotoContentTypeException ex)
			{
				return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status415UnsupportedMediaType };
			}
			catch (PhotoTooLargeException ex)
			{
				return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status413PayloadTooLarge };
			}
			catch (ArgumentException ex)
			{
				return new BadRequestObjectResult(ex.Message);
			}
			catch (Exception ex)
			{
				return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
			}
		}

		/// <summary>
		/// Retrieves a member's profile photo bytes.
		/// </summary>
		/// <param name="accountGuid">The account whose photo is being fetched.</param>
		/// <response code="200">Returns the photo bytes with the correct content type.</response>
		/// <response code="404">The member was not found, or has no photo set.</response>
		[HttpGet("{accountGuid:guid}")]
		public async Task<IActionResult> GetPhoto(Guid accountGuid)
		{
			try
			{
				var photo = await _membersService.GetMemberPhotoAsync(accountGuid);

				return photo is null ? new NotFoundResult() : File(photo.Value.Bytes, photo.Value.ContentType);
			}
			catch (MemberNotFoundException ex)
			{
				return new NotFoundObjectResult(ex.Message);
			}
			catch (Exception ex)
			{
				return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
			}
		}

		/// <summary>
		/// Removes a member's profile photo. Allowed for the member themselves or an Admin.
		/// </summary>
		/// <param name="accountGuid">The account whose photo is being removed.</param>
		/// <response code="200">Photo removed successfully.</response>
		/// <response code="400">The removal failed.</response>
		/// <response code="401">The caller's token claims are invalid.</response>
		/// <response code="403">The caller may not change this member's photo.</response>
		/// <response code="404">The target member was not found.</response>
		[HttpDelete("{accountGuid:guid}")]
		public async Task<ActionResult<bool>> DeletePhoto(Guid accountGuid)
		{
			try
			{
				var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

				if (string.IsNullOrEmpty(userIdClaim)
				    || !Guid.TryParse(userIdClaim, out var callerId))
				{
					return new UnauthorizedObjectResult("Invalid token claims");
				}

				var result = await _membersService.DeleteMemberPhotoAsync(accountGuid, callerId);

				return result ? new OkObjectResult(true) : new BadRequestObjectResult("Failed to remove photo");
			}
			catch (MemberPhotoAccessDeniedException ex)
			{
				return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
			}
			catch (MemberNotFoundException ex)
			{
				return new NotFoundObjectResult(ex.Message);
			}
			catch (Exception ex)
			{
				return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
			}
		}
	}
}
