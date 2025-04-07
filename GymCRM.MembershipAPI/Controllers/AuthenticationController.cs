using System.Security.Authentication;
using Asp.Versioning;
using GymCRM.MembershipAPI.Models.DTOs;
using GymCRM.MembershipAPI.Services.Interface;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace GymCRM.MembershipAPI.Controllers;

[EnableCors("AllowAny")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[action]")]
[ApiController]
public class AuthenticationController : ControllerBase
{
	private readonly IAuthenticationService _authenticationService;
	private ResponseDto _responseDto;

	public AuthenticationController(IAuthenticationService authenticationService)
	{
		_authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
		_responseDto = new ResponseDto();
	}

	[HttpPost]
	public ActionResult<Guid> Register([FromBody] AccountDto accountDto)
	{
		try
		{
			var registeredAccountGuid = _authenticationService.RegisterAccount(accountDto);

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
	public ActionResult Login([FromBody] AuthenticationRequestBody authenticationRequest)
	{
		try
		{
			var tokenToReturn = _authenticationService.LoginAccount(authenticationRequest);

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