using System.Security.Authentication;
using GymCRM.MembershipAPI.Infrastructure;
using GymCRM.MembershipAPI.Models.DTOs;

namespace GymCRM.MembershipAPI.Services.Interface;

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
	Task<string> LoginAccount(AuthenticationRequestBody accountDto, CancellationToken cancellationToken = default);
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
}