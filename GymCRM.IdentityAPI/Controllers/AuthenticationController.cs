using System.Security.Authentication;
using Asp.Versioning;
using GymCRM.IdentityAPI.Models.DTOs;
using GymCRM.IdentityAPI.Services.Interface;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace GymCRM.IdentityAPI.Controllers;

[EnableCors("AllowAny")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[action]")]
[ApiController]
public class AuthenticationController : ControllerBase
{
	private readonly IAuthenticationService _authenticationService;

	public AuthenticationController(IAuthenticationService authenticationService)
	{
		_authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
	}

	/// <summary>
	/// Registers a new account with the provided account details.
	/// </summary>
	/// <param name="insertAccount">The details of the account to be registered.</param>
	/// <returns>
	/// A <see cref="CreatedResult"/> with the newly created account's GUID if successful,
	/// a <see cref="StatusCodeResult"/> with 500 status code if account creation fails,
	/// or a <see cref="BadRequestResult"/> if an exception occurs.
	/// </returns>
	/// <response code="201">Returns the GUID of the newly created account.</response>
	/// <response code="500">Indicates that account registration failed.</response>
	/// <response code="400">Indicates an unexpected error occurred.</response>
	[HttpPost]
	public async Task<ActionResult<Guid>> Register([FromBody] InsertAccount insertAccount)
	{
		try
		{
			var registeredAccountGuid = await _authenticationService.RegisterAccount(insertAccount);

			if (registeredAccountGuid == Guid.Empty)
			{
				return new StatusCodeResult(StatusCodes.Status500InternalServerError);
			}

			return new CreatedResult();
		}
		catch (Exception)
		{
			return new BadRequestResult();
		}
	}

	/// <summary>
	/// Authenticates an account and returns a JWT token if credentials are valid.
	/// </summary>
	/// <param name="authenticationRequest">The account login credentials.</param>
	/// <returns>
	/// A <see cref="JsonResult"/> with the JWT token if login is successful,
	/// an <see cref="UnauthorizedObjectResult"/> with a message if authentication fails,
	/// or a <see cref="StatusCodeResult"/> with 500 status code if an error occurs.
	/// </returns>
	/// <response code="200">Returns a JWT token upon successful authentication.</response>
	/// <response code="401">Indicates that the authentication credentials are invalid.</response>
	/// <response code="500">Indicates an unexpected error occurred.</response>
	[HttpPost]
	public async Task<ActionResult> Login([FromBody] AuthenticationRequestBody authenticationRequest)
	{
		try
		{
			var tokenToReturn = await _authenticationService.LoginAccount(authenticationRequest);

			return new JsonResult(tokenToReturn);
		}
		catch (AuthenticationException ex)
		{
			return new UnauthorizedObjectResult(ex.Message);
		}
		catch (Exception)
		{
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}
}