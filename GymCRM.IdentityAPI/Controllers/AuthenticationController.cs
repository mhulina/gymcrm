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

	[HttpPost]
	public async Task<ActionResult<Guid>> Register([FromBody] InsertAccount insertAccount)
	{
		try
		{
			var registeredAccountGuid = await _authenticationService.RegisterAccount(insertAccount);

			if (registeredAccountGuid == Guid.Empty)
			{
				return new StatusCodeResult(500);
			}

			return new CreatedResult();
		}
		catch (Exception)
		{
			return new BadRequestResult();
		}
	}

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
			return new StatusCodeResult(500);
		}
	}
}