using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using GymCRM.MembershipAPI.Infrastructure;
using GymCRM.MembershipAPI.Infrastructure.Entities;
using GymCRM.MembershipAPI.Infrastructure.Interface;
using GymCRM.MembershipAPI.Models.DTOs;
using GymCRM.MembershipAPI.Services.Interface;
using ILogger = Serilog.ILogger;

namespace GymCRM.MembershipAPI.Services.Implementation;

public class AccountsService : IAccountsService
{
    private readonly IAccountsRepository _accountsRepository;
    private readonly IMembersRepository _membersRepository;
    private readonly ILogger _logger;

    public AccountsService(
        IAccountsRepository accountsRepository,
        IMembersRepository membersRepository,
        ILogger logger)
    {
        _accountsRepository = accountsRepository;
        _membersRepository = membersRepository;
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

    public AuthenticationResult LoginAccount(AuthenticationRequestBody accountDto)
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
                .FirstOrDefault();

            if (account is null)
            {
                throw new AuthenticationException("Account with this email does not exist");
            }

            var passwordsAreTheSame = CompareHashedPasswords(account, accountDto.Password);
            var authenticationResult = new AuthenticationResult
            {
                Success = passwordsAreTheSame,
                AccountDto = passwordsAreTheSame 
                    ? new AccountDto
                    {
                        Guid = account.Guid,
                        Email = account.Email
                    } 
                    : null,
            };
            
            return authenticationResult;
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

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public AccountDto AccountDto { get; set; }
    }
}