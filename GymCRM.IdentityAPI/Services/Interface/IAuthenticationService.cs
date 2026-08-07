using System.Security.Authentication;
using GymCRM.IdentityAPI.Models;
using GymCRM.IdentityAPI.Models.DTOs;
using Microsoft.AspNetCore.Authentication;

namespace GymCRM.IdentityAPI.Services.Interface;

public interface IAuthenticationService
{
	/// <summary>
	/// Registers a new account and its associated member in the system asynchronously.
	/// </summary>
	/// <param name="insertAccount">The account information to register, including email and password.</param>
	/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
	/// <returns>
	/// A task representing the asynchronous operation, containing the <see cref="Guid"/> of the newly created account.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown when the email or password is null or whitespace.</exception>
	/// <exception cref="AccountAlreadyExistsException">Thrown when an account with the provided email already exists.</exception>
	/// <exception cref="Exception">Thrown when account creation or persistence fails.</exception>
	Task<Guid> RegisterAccount(InsertAccount insertAccount, CancellationToken cancellationToken = default);
	/// <summary>
	/// Authenticates a user by validating their credentials and generates a JWT token upon successful login asynchronously.
	/// </summary>
	/// <param name="accountDto">The authentication request containing the username (email) and password.</param>
	/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
	/// <returns>
	/// A task representing the asynchronous operation, containing a JWT token as a string upon successful authentication.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown when the username or password is null or whitespace.</exception>
	/// <exception cref="AuthenticationException">
	/// Thrown when the account does not exist or when the provided password is incorrect.
	/// </exception>
	Task<(string accessToken, string refreshToken)> LoginAccount(
		AuthenticationRequestBody accountDto, 
		CancellationToken cancellationToken = default);
	/// <summary>
	/// Deletes an account from the system asynchronously using the provided account GUID.
	/// </summary>
	/// <param name="accountGuid">The GUID of the account to delete.</param>
	/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
	/// <returns>
	/// A task representing the asynchronous operation, containing true if the deletion was successful; otherwise, false.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown when the provided GUID is empty.</exception>
	Task<bool> DeleteAccount(Guid accountGuid, CancellationToken cancellationToken = default);
	/// <summary>
	/// Changes the password for an existing account, given a valid email and current password.
	/// </summary>
	/// <param name="email">The email address associated with the account.</param>
	/// <param name="oldPassword">The current password of the account.</param>
	/// <param name="newPassword">The new password to set for the account.</param>
	/// <param name="cancellationToken">Optional cancellation token for the async operation.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains a boolean indicating
	/// whether the password change was successful.
	/// </returns>
	/// <exception cref="ArgumentException">
	/// Thrown when the email, old password, or new password is null, empty, or whitespace.
	/// </exception>
	/// <exception cref="AccountDoesntExistException">
	/// Thrown when no account is found with the provided email.
	/// </exception>
	/// <exception cref="AuthenticationFailureException">
	/// Thrown when the provided old password does not match the stored password.
	/// </exception>
	Task<bool> ChangePassword(
		string email,
		string oldPassword,
		string newPassword,
		CancellationToken cancellationToken = default);
	/// <summary>
	/// Generates a JWT token for the specified <see cref="Models.Entities.Account"/> using application configuration for signing.
	/// </summary>
	/// <param name="account">The <see cref="Models.Entities.Account"/> for which to generate the token.</param>
	/// <returns>A JWT token as a string.</returns>
	/// <exception cref="Exception">Thrown when the signing secret is missing in the configuration.</exception>
	string GenerateJwtToken(Models.Entities.Account account);
	/// <summary>
	/// Checks whether any Admin account already exists - used to gate the first-run admin setup screen.
	/// </summary>
	/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
	Task<bool> HasAdminAccountAsync(CancellationToken cancellationToken = default);
	/// <summary>
	/// Creates the first Admin account. Re-checks server-side that no admin exists yet immediately
	/// before creating one, so this can only ever succeed once.
	/// </summary>
	/// <param name="request">The email/password (and optional detected timezone) for the new admin.</param>
	/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
	/// <returns>
	/// A task representing the asynchronous operation, containing the <see cref="Guid"/> of the newly created account.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown when the email or password is null or whitespace.</exception>
	/// <exception cref="AdminAccountAlreadyExistsException">Thrown when an admin account already exists.</exception>
	Task<Guid> SetupAdminAccountAsync(SetupAdminAccount request, CancellationToken cancellationToken = default);
}