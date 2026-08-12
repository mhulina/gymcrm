using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GymCRM.IdentityAPI.Infrastructure.Interface;
using GymCRM.IdentityAPI.Models;
using GymCRM.IdentityAPI.Models.DTOs;
using GymCRM.IdentityAPI.Models.Entities;
using GymCRM.IdentityAPI.Models.Enums;
using GymCRM.IdentityAPI.Models.Exceptions;
using GymCRM.IdentityAPI.Models.Interface;
using GymCRM.IdentityAPI.Services.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Account = GymCRM.IdentityAPI.Models.Entities.Account;
using AccountAccessDeniedException = GymCRM.IdentityAPI.Models.Exceptions.AccountAccessDeniedException;
using IAuthenticationService = GymCRM.IdentityAPI.Services.Interface.IAuthenticationService;
using ILogger = Serilog.ILogger;
using Member = GymCRM.IdentityAPI.Models.Entities.Member;

namespace GymCRM.IdentityAPI.Services.Implementation;

public class AuthenticationService : IAuthenticationService
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly IAccountsRepository _accountsRepository;
	private readonly IMembersRepository _membersRepository;
	private readonly IRefreshTokenService _refreshTokenService;
	private readonly IConfiguration _configuration;
	private readonly IPasswordHasher<Account> _passwordHasher;
	private readonly ILogger _logger;

	public AuthenticationService(
		IUnitOfWork unitOfWork,
		IAccountsRepository accountsRepository,
		IMembersRepository membersRepository,
		IRefreshTokenService refreshTokenService,
		IConfiguration configuration,
		IPasswordHasher<Account> passwordHasher,
		ILogger logger)
	{
		_unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
		_accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
		_membersRepository = membersRepository ?? throw new ArgumentNullException(nameof(membersRepository));
		_refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
		_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		_passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
		_logger = logger;
	}

	public Task<Guid> RegisterAccount(
		InsertAccount insertAccount,
		CancellationToken cancellationToken = default) =>
		RegisterAccountCore(insertAccount, mustChangePassword: false, cancellationToken);

	public async Task<Guid> AdminCreateAccountAsync(
		InsertAccount insertAccount,
		Guid callerAccountGuid,
		CancellationToken cancellationToken = default)
	{
		var caller = (await _membersRepository
			.FetchByCondition(x => x.AccountGuid == callerAccountGuid, cancellationToken))
			.FirstOrDefault();

		if (caller is null || caller.AccountType != (int)AccountType.Admin)
		{
			var ex = new AccountAccessDeniedException();
			_logger.Warning(ex, "Blocked admin account creation attempt by non-admin caller {CallerAccountGuid}", callerAccountGuid);
			throw ex;
		}

		return await RegisterAccountCore(insertAccount, mustChangePassword: true, cancellationToken);
	}

	// mustChangePassword is true only for accounts created by RegisterAccount's admin-facing
	// counterpart (AdminCreateAccountAsync) - a password an admin assigns on someone else's
	// behalf is temporary by definition, and must be flagged so ChangePassword can clear it once
	// the account's real owner sets their own.
	private async Task<Guid> RegisterAccountCore(
		InsertAccount insertAccount,
		bool mustChangePassword,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(insertAccount.Email)
			|| string.IsNullOrWhiteSpace(insertAccount.Password))
		{
			throw new ArgumentException("Email and/or password is required");
		}

		try
		{
			var accountExists = (await _accountsRepository
				.FetchByConditionAsync(x => string.Equals(x.Email, insertAccount.Email), cancellationToken))
				.Any();

			if (accountExists)
			{
				throw new AccountAlreadyExistsException();
			}

			var entity = CreateAccountWithHashedPassword(insertAccount, mustChangePassword);
			_accountsRepository.Insert(entity);

			var result = await _unitOfWork.SaveAsync(cancellationToken);

			if (!result)
			{
				throw new Exception("Failed to add account");
			}

			var member = new Member
			{
				Id = Guid.CreateVersion7(),
				AccountGuid = entity.Id,
				Email = insertAccount.Email.ToLower(),
				AccountType = insertAccount.AccountType ?? 1,
				GymSubscriptionType = insertAccount.GymSubscriptionType ?? 0,
				Gender = insertAccount.Gender ?? 0,
				DateModified = entity.DateCreated,
				TimeZone = string.IsNullOrWhiteSpace(insertAccount.TimeZone)
					? TimeZoneInfo.Utc.Id
					: insertAccount.TimeZone,
			};

			_membersRepository.Insert(member);
			await _unitOfWork.SaveAsync(cancellationToken);

			return entity.Id;
		}
		catch (Exception ex)
		{
			_logger.Error(ex, ex.Message);

			throw;
		}
	}

	public Task<bool> HasAdminAccountAsync(CancellationToken cancellationToken = default) =>
		_membersRepository.AnyByAccountTypeAsync((int)AccountType.Admin, cancellationToken);

	public async Task<Guid> SetupAdminAccountAsync(
		SetupAdminAccount request,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(request.Email)
			|| string.IsNullOrWhiteSpace(request.Password))
		{
			throw new ArgumentException("Email and/or password is required");
		}

		var adminExists = await _membersRepository.AnyByAccountTypeAsync((int)AccountType.Admin, cancellationToken);

		if (adminExists)
		{
			var ex = new AdminAccountAlreadyExistsException();
			_logger.Warning(ex, "Blocked admin setup attempt for {Email} - an admin account already exists", request.Email);

			throw ex;
		}

		// Reuses RegisterAccount rather than duplicating account/member creation - this is
		// purely additive, RegisterAccount/POST Register are untouched by this feature.
		return await RegisterAccount(new InsertAccount
		{
			Email = request.Email,
			Password = request.Password,
			AccountType = (int)AccountType.Admin,
			GymSubscriptionType = 0,
			Gender = 0,
			TimeZone = request.TimeZone,
		}, cancellationToken);
	}

	public async Task<(string accessToken, string refreshToken, bool mustChangePassword)> LoginAccount(
		AuthenticationRequestBody accountDto, 
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(accountDto.Username)
			|| string.IsNullOrWhiteSpace(accountDto.Password))
		{
			throw new ArgumentException("Email and/or password is required");
		}

		try
		{
			var account = (await _accountsRepository
				.FetchByConditionAsync(x => string.Equals(x.Email, accountDto.Username), cancellationToken))
				.FirstOrDefault()
				?? throw new AuthenticationException("Account with this email does not exist");

			if (account.LockoutUntil.HasValue
			    && account.LockoutUntil > DateTime.UtcNow)
			{
				var remainingTime = account.LockoutUntil.Value - DateTime.UtcNow;
				throw new AuthenticationException($"Account locked. Try again in {remainingTime.Minutes} minutes");
			}

			var (passwordsAreTheSame, needsRehash) = VerifyPassword(account, accountDto.Password);

			if (!passwordsAreTheSame)
			{
				account.FailedLoginAttempts++;

				if (account.FailedLoginAttempts >= 5)
				{
					account.LockoutUntil = DateTime.UtcNow.AddMinutes(15);
					_logger.Warning(
						"Account {Email} locker after {LoginAttempts} failed login attempts",
						account.Email,
						account.FailedLoginAttempts);
				}
				
				_accountsRepository.Update(account);
				await _unitOfWork.SaveAsync(cancellationToken);
				
				throw new AuthenticationException("Invalid credentials");
			}

			account.FailedLoginAttempts = 0;
			account.LockoutUntil = null;

			// Transparent upgrade: a legacy (pre-PBKDF2) account that just proved it knows the
			// right password gets rehashed into the new format here, piggybacking on the update
			// below rather than a separate write - old accounts migrate on next login, nobody
			// is forced to reset anything.
			if (needsRehash)
			{
				account.HashedPassword = HashPasswordWithPepper(account, accountDto.Password);
			}

			_accountsRepository.Update(account);
			await _unitOfWork.SaveAsync(cancellationToken);

			var refreshToken = _refreshTokenService.GenerateRefreshToken(account.Id);
			var accessToken = GenerateJwtToken(account);
			
			var result = await _refreshTokenService.SaveRefreshTokenAsync(
				refreshToken, 
				cancellationToken: cancellationToken);

			if (result)
			{
				return (accessToken, refreshToken.Token, account.MustChangePassword);
			}
			
			_logger.Warning(
				"Failed to refresh token for account ID: {AccountId}",
				account.Id);

			throw new AuthenticationFailureException($"Failed to refresh token for account ID: {account.Id}");
		}
		catch (Exception ex)
		{
			_logger.Error(ex, ex.Message);
			throw;
		}
	}
	
	public async Task<bool> DeleteAccount(Guid accountGuid, CancellationToken cancellationToken = default)
	{
		if (accountGuid == Guid.Empty)
		{
			throw new ArgumentException($"{accountGuid} is not a valid GUID");
		}

		try
		{
			_accountsRepository.Delete(new Account { Id = accountGuid });
			var result = await _unitOfWork.SaveAsync(cancellationToken);

			return result;
		}
		catch (Exception ex)
		{
			_logger.Error(ex, ex.Message);

			throw;
		}
	}

	public async Task<bool> ChangePassword(
		string email, 
		string oldPassword, 
		string newPassword,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(email.Trim().ToLower()))
		{
			throw new ArgumentException("Email is required");
		}

		if (string.IsNullOrWhiteSpace(oldPassword.Trim().ToLower())
		    || string.IsNullOrWhiteSpace(newPassword.Trim().ToLower()))
		{
			throw new ArgumentException("Old password and/or password is required");
		}
		
		var modifiedEmail = email.Trim().ToLower();
		
		var account = (await _accountsRepository.FetchByConditionAsync(x => x.Email == modifiedEmail, cancellationToken))
			.FirstOrDefault() ?? throw new AccountDoesntExistException();

		var (oldPasswordIsValid, _) = VerifyPassword(account, oldPassword);

		if (!oldPasswordIsValid)
		{
			throw new AuthenticationFailureException("Password is invalid");
		}

		if (oldPassword == newPassword)
		{
			throw new ArgumentException("New password must be different from the current password");
		}

		account.HashedPassword = HashPasswordWithPepper(account, newPassword);
		account.MustChangePassword = false;

		_unitOfWork.Detach(account);
		_accountsRepository.Update(account);
		var result = await _unitOfWork.SaveAsync(cancellationToken);

		// Update() re-attaches the account (tracked, Unchanged after save) - leave the change
		// tracker clean so a caller chaining another fetch of the same account in the same
		// scope right after (e.g. AuthenticationController.ChangePassword calling LoginAccount
		// to reissue cookies) doesn't hit an EF "entity with this key is already tracked"
		// conflict.
		_unitOfWork.DetachAll();

		return result;
	}
	
	public string GenerateJwtToken(Account account)
	{
		if (account is null)
		{
			throw new ArgumentNullException(nameof(account));
		}
		
		var securityKey = new SymmetricSecurityKey(
			Convert.FromBase64String(
				_configuration["Authentication:SecretForKey"]
				?? throw new Exception("No existing secret for key")));
		var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

		var claimsForToken = new List<Claim>
		{
			new ("sub", account.Id.ToString()),
			new ("email", account.Email),
			new ("type", ((AccountType)account.Member.AccountType).ToString()),
			new ("timezone", account.Member.TimeZone),
			new ("mustChangePassword", account.MustChangePassword.ToString())
		};

		var jwtSecurityToken = new JwtSecurityToken(
			_configuration["Authentication:Issuer"],
			_configuration["Authentication:Audience"],
			claimsForToken,
			DateTime.UtcNow,
			DateTime.UtcNow.AddMinutes(30),
			signingCredentials);

		var tokenToReturn = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
		
		return tokenToReturn;
	}

	/// <summary>
	/// Creates a new <see cref="Models.Entities.Account"/> entity with a hashed password (PBKDF2-SHA256
	/// via <see cref="_passwordHasher"/>, peppered with a generated per-account salt).
	/// </summary>
	/// <param name="insertAccount">The account information containing the email and password to hash.</param>
	/// <param name="mustChangePassword">Whether this account's password was assigned by someone else and must be changed on first use.</param>
	/// <returns>
	/// A new <see cref="Models.Entities.Account"/> entity with hashed password, salt, and creation metadata.
	/// </returns>
	private Account CreateAccountWithHashedPassword(InsertAccount insertAccount, bool mustChangePassword)
	{
		var hashPepper = RandomNumberGenerator.GetHexString(25);
		var dateCreated = DateTime.UtcNow;
		var accountGuid = Guid.CreateVersion7();

		var entity = new Account
		{
			Id = accountGuid,
			Email = insertAccount.Email.ToLower(),
			DateCreated = dateCreated,
			HashPepper = hashPepper,
			MustChangePassword = mustChangePassword
		};
		entity.HashedPassword = HashPasswordWithPepper(entity, insertAccount.Password);

		return entity;
	}

	/// <summary>
	/// Verifies a plaintext password against an account's stored hash. Understands both the
	/// current PBKDF2-SHA256 format and the legacy hand-rolled HMACSHA256 format that accounts
	/// created before this migration still carry - <paramref name="needsRehash"/> is <c>true</c>
	/// exactly when a legacy hash matched, so the caller can transparently upgrade it.
	/// </summary>
	private (bool isValid, bool needsRehash) VerifyPassword(Account account, string providedPassword)
	{
		var pepperedPassword = account.HashPepper + providedPassword;

		// IPasswordHasher.VerifyHashedPassword doesn't throw for a hash that isn't in its
		// format (confirmed empirically) - it returns Failed just like a genuine mismatch would,
		// so a legacy hash always falls through to the check below rather than short-circuiting.
		try
		{
			var result = _passwordHasher.VerifyHashedPassword(account, account.HashedPassword, pepperedPassword);

			if (result != PasswordVerificationResult.Failed)
			{
				return (true, result == PasswordVerificationResult.SuccessRehashNeeded);
			}
		}
		catch (Exception)
		{
			// Defensive: fall through to the legacy check below regardless of how a
			// not-actually-PBKDF2 stored value fails to verify.
		}

		var legacyHash = GenerateLegacyHashedPassword(providedPassword, account.HashPepper, account.DateCreated);

		return legacyHash == account.HashedPassword ? (true, true) : (false, false);
	}

	private string HashPasswordWithPepper(Account account, string password) =>
		_passwordHasher.HashPassword(account, account.HashPepper + password);

	/// <summary>
	/// The original (pre-migration) hashing scheme - HMACSHA256 keyed by the account's salt, over
	/// salt+dateCreated+password. Kept only so <see cref="VerifyPassword"/> can still authenticate
	/// accounts that haven't logged in (and therefore been transparently rehashed) since the move
	/// to <see cref="_passwordHasher"/>. Never used for new hashes.
	/// </summary>
	private static string GenerateLegacyHashedPassword(string password, string salt, DateTime dateCreated)
	{
		var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(salt));
		hmac.Initialize();
		var hashedPassword = Convert.ToBase64String(
			hmac.ComputeHash(
				Encoding.UTF8.GetBytes(salt + dateCreated + password)));

		return hashedPassword;
	}
}