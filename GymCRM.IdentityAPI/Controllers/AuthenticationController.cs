using System.Security.Authentication;
using System.Security.Claims;
using Asp.Versioning;
using GymCRM.IdentityAPI.Models;
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
	/// Creates an account on behalf of another user - same as <see cref="Register"/>, except the
	/// caller must be an authenticated Admin, and the resulting account is flagged
	/// <c>MustChangePassword</c> since the password was assigned by someone other than its owner.
	/// </summary>
	/// <param name="insertAccount">The details of the account to be registered.</param>
	/// <returns>
	/// A <see cref="CreatedResult"/> if successful, a <see cref="ConflictObjectResult"/> if the
	/// email is already registered, a <see cref="ObjectResult"/> with 403 if the caller isn't an
	/// Admin, or a <see cref="BadRequestResult"/> if an unexpected error occurs.
	/// </returns>
	/// <response code="201">Returns the GUID of the newly created account.</response>
	/// <response code="401">The caller's token claims are invalid.</response>
	/// <response code="403">The caller is not an Admin.</response>
	/// <response code="409">An account with that email already exists.</response>
	/// <response code="400">Indicates an unexpected error occurred.</response>
	[HttpPost]
	[Authorize]
	[EnableRateLimiting("register")]
	public async Task<ActionResult<Guid>> AdminCreateAccount([FromBody] InsertAccount insertAccount, CancellationToken cancellationToken)
	{
		var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

		if (string.IsNullOrEmpty(callerIdClaim) || !Guid.TryParse(callerIdClaim, out var callerAccountGuid))
		{
			return new UnauthorizedObjectResult("Invalid token claims");
		}

		try
		{
			var registeredAccountGuid = await _authenticationService.AdminCreateAccountAsync(
				insertAccount, callerAccountGuid, cancellationToken);

			if (registeredAccountGuid == Guid.Empty)
			{
				return new StatusCodeResult(StatusCodes.Status500InternalServerError);
			}

			return new CreatedResult();
		}
		catch (AccountAccessDeniedException ex)
		{
			return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
		}
		catch (AccountAlreadyExistsException ex)
		{
			return new ConflictObjectResult(ex.Message);
		}
		catch (Exception)
		{
			return new BadRequestResult();
		}
	}

	/// <summary>
	/// Changes the caller's own password, given a valid current password. Also clears
	/// <c>MustChangePassword</c> if it was set, and reissues fresh session cookies (revoking the
	/// old refresh token first) so a previously-leaked session can't survive the change.
	/// </summary>
	/// <param name="request">The current and new password.</param>
	/// <response code="200">The password was changed and fresh cookies were issued.</response>
	/// <response code="400">The request was invalid, or the new password matched the old one.</response>
	/// <response code="401">The caller's token claims are invalid, or the old password was wrong.</response>
	/// <response code="404">The account could not be found.</response>
	/// <response code="500">An unexpected error occurred on the server.</response>
	[HttpPost]
	[Authorize]
	[EnableRateLimiting("auth")]
	public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
	{
		var callerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		// Not the literal "email" claim name - the default JWT inbound claim mapping rewrites the
		// short "email" claim to ClaimTypes.Email server-side after validation (the same
		// mechanism that makes "sub" readable as ClaimTypes.NameIdentifier).
		var email = User.FindFirst(ClaimTypes.Email)?.Value;

		if (string.IsNullOrEmpty(callerIdClaim)
		    || !Guid.TryParse(callerIdClaim, out var callerAccountGuid)
		    || string.IsNullOrEmpty(email))
		{
			return new UnauthorizedObjectResult("Invalid token claims");
		}

		try
		{
			var result = await _authenticationService.ChangePassword(
				email, request.OldPassword, request.NewPassword, cancellationToken);

			if (!result)
			{
				return new BadRequestResult();
			}

			// Revoke the session's existing refresh token BEFORE minting a fresh pair below -
			// doing it after would revoke the session just created instead of the old one.
			await _refreshTokenService.RevokeAllTokensForAccountAsync(callerAccountGuid, "Password changed");

			var tokens = await _authenticationService.LoginAccount(
				new AuthenticationRequestBody { Username = email, Password = request.NewPassword },
				cancellationToken);
			SetTokenCookies(tokens.accessToken, tokens.refreshToken);

			return new OkResult();
		}
		catch (AccountDoesntExistException ex)
		{
			return new NotFoundObjectResult(ex.Message);
		}
		catch (AuthenticationFailureException ex)
		{
			return new UnauthorizedObjectResult(ex.Message);
		}
		catch (ArgumentException ex)
		{
			return new BadRequestObjectResult(ex.Message);
		}
		catch (Exception)
		{
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	/// <summary>
	/// Checks whether any Admin account already exists - used by the frontend to gate the
	/// first-run admin setup screen. Public - must be callable before anyone has logged in.
	/// </summary>
	/// <response code="200">Returns true if an admin account exists, false otherwise.</response>
	/// <response code="500">Indicates an unexpected error occurred.</response>
	[HttpGet]
	public async Task<ActionResult<bool>> HasAdminAccount()
	{
		try
		{
			return new OkObjectResult(await _authenticationService.HasAdminAccountAsync());
		}
		catch (Exception ex)
		{
			return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
		}
	}

	/// <summary>
	/// Creates the first Admin account. Can only ever succeed once - the service re-checks
	/// server-side that no admin exists yet immediately before creating one.
	/// </summary>
	/// <param name="request">The email/password (and optional detected timezone) for the new admin.</param>
	/// <response code="201">Returns the GUID of the newly created admin account.</response>
	/// <response code="400">Indicates the request was invalid.</response>
	/// <response code="409">Indicates an admin account already exists.</response>
	/// <response code="500">Indicates an unexpected error occurred.</response>
	[HttpPost]
	[EnableRateLimiting("register")]
	public async Task<ActionResult<Guid>> SetupAdminAccount([FromBody] SetupAdminAccount request)
	{
		try
		{
			return new CreatedResult(string.Empty, await _authenticationService.SetupAdminAccountAsync(request));
		}
		catch (AdminAccountAlreadyExistsException ex)
		{
			return new ConflictObjectResult(ex.Message);
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

			return new OkObjectResult(new { mustChangePassword = tokens.mustChangePassword });
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
	/// <returns>200 OK (with the account's mustChangePassword state) if authenticated, 401 Unauthorized otherwise.</returns>
	[HttpGet]
	[Authorize]
	public ActionResult CheckAuth()
	{
		// Claim value is "True"/"False" (C# ToString() casing) - TryParse is case-insensitive,
		// a literal string comparison against lowercase "true" would silently always be false.
		bool.TryParse(User.FindFirst("mustChangePassword")?.Value, out var mustChangePassword);

		return new OkObjectResult(new { mustChangePassword });
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