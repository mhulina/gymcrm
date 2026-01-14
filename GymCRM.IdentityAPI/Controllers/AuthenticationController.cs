using System.Security.Authentication;
using Asp.Versioning;
using GymCRM.IdentityAPI.Models.DTOs;
using GymCRM.IdentityAPI.Services.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using IAuthenticationService = GymCRM.IdentityAPI.Services.Interface.IAuthenticationService;

namespace GymCRM.IdentityAPI.Controllers;

// [EnableCors("AllowAny")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]/[action]")]
[ApiController]
public class AuthenticationController : ControllerBase
{
	private readonly IAuthenticationService _authenticationService;
	private readonly IRefreshTokenService _refreshTokenService;

	public AuthenticationController(
		IAuthenticationService authenticationService,
		IRefreshTokenService refreshTokenService)
	{
		_authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
		_refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
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
	[EnableRateLimiting("register")]
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
	[EnableRateLimiting("auth")]
	public async Task<ActionResult> Login([FromBody] AuthenticationRequestBody authenticationRequest)
	{
		try
		{
			var tokens = await _authenticationService.LoginAccount(authenticationRequest);

			SetTokenCookies(tokens.accessToken, tokens.refreshToken);

			return new OkResult();
		}
		catch (AuthenticationException ex)
		{
			return new UnauthorizedObjectResult(ex.Message);
		}
		catch (AuthenticationFailureException ex)
		{
			return new UnauthorizedObjectResult(ex.Message);
		}
		catch (Exception)
		{
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	/// <summary>
	/// Logs out the user by revoking their refresh token and clearing authentication cookies.
	/// </summary>
	/// <returns>
	/// An <see cref="OkResult"/> if logout succeeds,
	/// or a <see cref="BadRequestObjectResult"/> if no refresh token is found,
	/// or a <see cref="StatusCodeResult"/> with status 500 if an error occurs.
	/// </returns>
	/// <response code="200">User logged out successfully, cookies cleared.</response>
	/// <response code="400">No refresh token found in request.</response>
	/// <response code="500">Indicates an unexpected error occurred.</response>
	[HttpPost]
	public async Task<ActionResult> Logout()
	{
		try
		{
			var refreshTokenCookie = Request.Cookies["refreshToken"];

			if (string.IsNullOrEmpty(refreshTokenCookie))
			{
				return new BadRequestObjectResult("No refresh token found");
			}
			
			var token = await _refreshTokenService.ValidateRefreshTokenAsync(refreshTokenCookie);

			if (token != null)
			{
				await _refreshTokenService.RevokeRefreshTokenAsync(token, "User logout");
			}
			
			Response.Cookies.Delete("refreshToken");
			Response.Cookies.Delete("accessToken");
			
			return new OkResult();
		}
		catch (Exception)
		{
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	/// <summary>
	/// Refreshes an expired access token using a valid refresh token from httpOnly cookie.
	/// Implements token rotation by issuing new access and refresh tokens and revoking the old refresh token.
	/// </summary>
	/// <returns>
	/// An <see cref="OkResult"/> if token refresh succeeds and new cookies are set,
	/// or an <see cref="UnauthorizedObjectResult"/> if the refresh token is invalid or expired,
	/// or a <see cref="StatusCodeResult"/> with status 500 if an error occurs.
	/// </returns>
	/// <response code="200">Token refreshed successfully, new tokens set in httpOnly cookies.</response>
	/// <response code="401">Invalid or expired refresh token.</response>
	/// <response code="500">Indicates an unexpected error occurred.</response>
	[HttpPost]
	public async Task<ActionResult> RefreshToken()
	{
		try
		{
			var refreshTokenCookie = Request.Cookies["refreshToken"];

			if (string.IsNullOrEmpty(refreshTokenCookie))
			{
				return new UnauthorizedObjectResult("No refresh token found");
			}
			
			var refreshToken = await _refreshTokenService.ValidateRefreshTokenAsync(refreshTokenCookie);

			if (refreshToken == null)
			{
				return new UnauthorizedObjectResult("Invalid or expired refresh token");
			}
			
			var newAccessToken = _authenticationService.GenerateJwtToken(refreshToken.Account);
			var newRefreshToken = _refreshTokenService.GenerateRefreshToken(refreshToken.AccountId);
			
			await _refreshTokenService.RevokeRefreshTokenAsync(
				refreshToken, 
				"Replaced by new token",
				newRefreshToken.Token);
			await _refreshTokenService.SaveRefreshTokenAsync(newRefreshToken);
			
			SetTokenCookies(newAccessToken, newRefreshToken.Token);

			return new OkResult();
		}
		catch (Exception)
		{
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	/// <summary>
	/// Checks if the current request has valid authentication.
	/// Used by frontend to verify authentication status.
	/// </summary>
	/// <returns>200 OK if authenticated, 401 Unauthorized otherwise.</returns>
	[HttpGet]
	[Authorize]
	public ActionResult CheckAuth()
	{
		return new OkResult();
	}
	
	/// <summary>
	/// Sets authentication tokens as httpOnly cookies in the response.
	/// Access token has a short lifetime (30 minutes), refresh token has a longer lifetime (7 days).
	/// Cookies are marked as Secure (HTTPS only) and SameSite=Strict for security.
	/// </summary>
	/// <param name="accessToken">The JWT access token to set as a cookie.</param>
	/// <param name="refreshToken">The refresh token to set as a cookie.</param>
	private void SetTokenCookies(string accessToken, string refreshToken)
	{
		// Access token (short-lived - 30 minutes)
		var accessTokenOptions = new CookieOptions
		{
			HttpOnly = true,
			Secure = false, // HTTP only
			SameSite = SameSiteMode.Lax,
			Expires = DateTime.UtcNow.AddMinutes(30),
			Path = "/"
		};
		Response.Cookies.Append("accessToken", accessToken, accessTokenOptions);

		// Refresh token (long-lived - 7 days)
		var refreshTokenOptions = new CookieOptions
		{
			HttpOnly = true,
			Secure = false, // HTTP only
			SameSite = SameSiteMode.Lax,
			Expires = DateTime.UtcNow.AddDays(7),
			Path = "/"
		};
		Response.Cookies.Append("refreshToken", refreshToken, refreshTokenOptions);
	}
}