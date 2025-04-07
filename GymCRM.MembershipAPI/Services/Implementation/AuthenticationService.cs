using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GymCRM.MembershipAPI.Infrastructure;
using GymCRM.MembershipAPI.Infrastructure.Entities;
using GymCRM.MembershipAPI.Infrastructure.Interface;
using GymCRM.MembershipAPI.Models.DTOs;
using GymCRM.MembershipAPI.Services.Interface;
using Microsoft.IdentityModel.Tokens;
using ILogger = Serilog.ILogger;

namespace GymCRM.MembershipAPI.Services.Implementation;

public class AuthenticationService : IAuthenticationService
{
	private readonly IAccountsRepository _accountsRepository;
	private readonly IMembersRepository _membersRepository;
	private readonly IConfiguration _configuration;
	private readonly ILogger _logger;

	public AuthenticationService(
		IAccountsRepository accountsRepository,
		IMembersRepository membersRepository,
		IConfiguration configuration,
		ILogger logger)
	{
		_accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
		_membersRepository = membersRepository ?? throw new ArgumentNullException(nameof(membersRepository));
		_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		_logger = logger;
	}

	public Guid RegisterAccount(AccountDto accountDto)
	{
		if (string.IsNullOrWhiteSpace(accountDto.Email)
			|| string.IsNullOrWhiteSpace(accountDto.Password))
		{
			throw new ArgumentException("Email and/or password is required");
		}

		var account = CreateAccountWithHashedPassword(accountDto);

		try
		{
			var accountExists = _accountsRepository
				.FetchByCondition(x => string.Equals(x.Email, accountDto.Email))
				.Any();

			if (accountExists)
			{
				throw new AccountAlreadyExistsException();
			}

			_accountsRepository.Insert(account);

			var result = _accountsRepository.Save();

			if (!result)
			{
				throw new Exception("Failed to add account");
			}

			var member = new Member
			{
				AccountGuid = account.Guid,
				Email = accountDto.Email.ToLower(),
				AccountType = accountDto.AccountType ?? 0,
				GymSubscriptionType = accountDto.GymSubscriptionType ?? 0,
				Gender = accountDto.Gender ?? 0,
			};

			_membersRepository.Insert(member);
			_membersRepository.Save();

			return account.Guid;
		}
		catch (Exception ex)
		{
			_logger.Error(ex, ex.Message);

			throw;
		}
	}

	public string LoginAccount(AuthenticationRequestBody accountDto)
	{
		if (string.IsNullOrWhiteSpace(accountDto.Username)
			|| string.IsNullOrWhiteSpace(accountDto.Password))
		{
			throw new ArgumentException("Email and/or password is required");
		}

		try
		{
			var account = _accountsRepository
				.FetchByCondition(x => string.Equals(x.Email, accountDto.Username))
				.FirstOrDefault()
				?? throw new AuthenticationException("Account with this email does not exist");

			var passwordsAreTheSame = CompareHashedPasswords(account, accountDto.Password);

			if (!passwordsAreTheSame)
			{
				throw new AuthenticationException("Password is incorrect");
			}

			var securityKey = new SymmetricSecurityKey(
				Convert.FromBase64String(
					_configuration["Authentication:SecretForKey"]
					?? throw new Exception("No existing secret for key")));
			var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

			var claimsForToken = new List<Claim>
			{
				new ("sub", account.Guid.ToString()),
				new ("email", account.Email)
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
		catch (Exception ex)
		{
			_logger.Error(ex, ex.Message);
			throw;
		}
	}

	public bool DeleteAccount(Guid accountGuid)
	{
		if (accountGuid == Guid.Empty)
		{
			throw new ArgumentException($"{accountGuid} is not a valid GUID");
		}

		try
		{
			_accountsRepository.Delete(new Account { Guid = accountGuid });
			var result = _accountsRepository.Save();

			return result;
		}
		catch (Exception ex)
		{
			_logger.Error(ex, ex.Message);

			throw;
		}
	}

	private Account CreateAccountWithHashedPassword(AccountDto accountDto)
	{
		var hashSalt = RandomNumberGenerator.GetHexString(25);
		var dateCreated = DateTime.UtcNow;
		var accountGuid = Guid.NewGuid();
		var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(hashSalt));
		hmac.Initialize();

		var account = new Account
		{
			Guid = accountGuid,
			Email = accountDto.Email.ToLower(),
			DateCreated = dateCreated,
			HashSalt = hashSalt,
			HashedPassword = Convert.ToBase64String(
				hmac.ComputeHash(
					Encoding.UTF8.GetBytes(hashSalt + dateCreated + accountDto.Password)))
		};

		return account;
	}

	private bool CompareHashedPasswords(Account account, string providedPassword)
	{
		var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(account.HashSalt));
		hmac.Initialize();
		var hashedProvidedPassword = Convert.ToBase64String(
			hmac.ComputeHash(
				Encoding.UTF8.GetBytes(account.HashSalt + account.DateCreated + providedPassword)));

		var passwordsAreTheSame = hashedProvidedPassword == account.HashedPassword;

		return passwordsAreTheSame;
	}
}