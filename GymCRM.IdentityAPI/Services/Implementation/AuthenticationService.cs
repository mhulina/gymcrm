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
using GymCRM.IdentityAPI.Models.Interface;
using GymCRM.IdentityAPI.Services.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using Account = GymCRM.IdentityAPI.Models.Entities.Account;
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
	private readonly ILogger _logger;

	public AuthenticationService(
		IUnitOfWork unitOfWork,
		IAccountsRepository accountsRepository,
		IMembersRepository membersRepository,
		IRefreshTokenService refreshTokenService,
		IConfiguration configuration,
		ILogger logger)
	{
		_unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
		_accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
		_membersRepository = membersRepository ?? throw new ArgumentNullException(nameof(membersRepository));
		_refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
		_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		_logger = logger;
	}

	public async Task<Guid> RegisterAccount(
		InsertAccount insertAccount, 
		CancellationToken cancellationToken = default)
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

			var entity = CreateAccountWithHashedPassword(insertAccount);
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
				TimeZone = TimeZoneInfo.Utc.Id,
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

	public async Task<(string accessToken, string refreshToken)> LoginAccount(
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

			var passwordsAreTheSame = CompareHashedPasswords(account, accountDto.Password);

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
			_accountsRepository.Update(account);
			await _unitOfWork.SaveAsync(cancellationToken);
			
			var refreshToken = _refreshTokenService.GenerateRefreshToken(account.Id);
			var accessToken = GenerateJwtToken(account);
			
			var result = await _refreshTokenService.SaveRefreshTokenAsync(
				refreshToken, 
				cancellationToken: cancellationToken);

			if (result)
			{
				return (accessToken, refreshToken.Token);
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

		if (!CompareHashedPasswords(account, oldPassword))
		{
			throw new AuthenticationFailureException("Password is invalid");
		}
		
		account.HashedPassword = GenerateHashedPassword(newPassword, account.HashSalt, account.DateCreated);
		
		_unitOfWork.Detach(account);
		_accountsRepository.Update(account);
		var result = await _unitOfWork.SaveAsync(cancellationToken);

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
			new ("timezone", account.Member.TimeZone)
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
	/// Creates a new <see cref="Models.Entities.Account"/> entity with a hashed password using HMACSHA256 and a generated salt.
	/// </summary>
	/// <param name="insertAccount">The account information containing the email and password to hash.</param>
	/// <returns>
	/// A new <see cref="Models.Entities.Account"/> entity with hashed password, salt, and creation metadata.
	/// </returns>
	private static Account CreateAccountWithHashedPassword(InsertAccount insertAccount)
	{
		var hashSalt = RandomNumberGenerator.GetHexString(25);
		var dateCreated = DateTime.UtcNow;
		var accountGuid = Guid.CreateVersion7();

		var entity = new Account
		{
			Id = accountGuid,
			Email = insertAccount.Email.ToLower(),
			DateCreated = dateCreated,
			HashSalt = hashSalt,
			HashedPassword = GenerateHashedPassword(insertAccount.Password, hashSalt, dateCreated)
		};

		return entity;
	}

	/// <summary>
	/// Compares the stored hashed password of an account with a provided plaintext password to verify authentication.
	/// </summary>
	/// <param name="account">The <see cref="Models.Entities.Account"/> containing the stored hash and salt.</param>
	/// <param name="providedPassword">The plaintext password provided by the user for authentication.</param>
	/// <returns>
	/// True if the hashed provided password matches the stored hash; otherwise, false.
	/// </returns>
	private static bool CompareHashedPasswords(Account account, string providedPassword)
	{
		var hashedProvidedPassword = GenerateHashedPassword(providedPassword, account.HashSalt, account.DateCreated);

		var passwordsAreTheSame = hashedProvidedPassword == account.HashedPassword;

		return passwordsAreTheSame;
	}

	private static string GenerateHashedPassword(string password, string salt, DateTime dateCreated)
	{
		var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(salt));
		hmac.Initialize();
		var hashedPassword = Convert.ToBase64String(
			hmac.ComputeHash(
				Encoding.UTF8.GetBytes(salt + dateCreated + password)));
		
		return hashedPassword;
	}
}