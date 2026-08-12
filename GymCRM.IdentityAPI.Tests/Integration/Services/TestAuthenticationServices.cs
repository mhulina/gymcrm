using GymCRM.IdentityAPI.Models.DTOs;
using GymCRM.IdentityAPI.Models.Interface;
using FluentAssertions;
using GymCRM.IdentityAPI.Infrastructure.Interface;
using GymCRM.IdentityAPI.Models;
using GymCRM.IdentityAPI.Models.Enums;
using GymCRM.IdentityAPI.Models.Exceptions;
using GymCRM.IdentityAPI.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymCRM.IdentityAPI.Tests.Integration.Services;

public class AuthenticationServiceTests : TestBase
{
    private IAuthenticationService _authenticationService;
    private IMembersRepository _membersRepository;
    private IAccountsRepository _accountsRepository;
    private IUnitOfWork _unitOfWork;

    public AuthenticationServiceTests()
    {
        _authenticationService = ServiceProvider.GetRequiredService<IAuthenticationService>();
        _membersRepository = ServiceProvider.GetRequiredService<IMembersRepository>();
        _accountsRepository = ServiceProvider.GetRequiredService<IAccountsRepository>();
        _unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Clear Accounts and Members tables for test isolation
        try
        {
            _context.Database
                .ExecuteSqlRawAsync("TRUNCATE TABLE \"Accounts\" RESTART IDENTITY CASCADE;")
                .GetAwaiter()
                .GetResult();
            _context.Database
                .ExecuteSqlRawAsync("TRUNCATE TABLE \"Members\" RESTART IDENTITY CASCADE;")
                .GetAwaiter()
                .GetResult();
        }
        catch
        {
            ClearDatabase();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GivenValidInsertAccount_WhenRegisteringAccount_ThenAccountAndMemberAreCreatedProperly()
    {
        // Arrange
        var email = $"user{Guid.NewGuid():N}@test.com";
        var insertAccount = new InsertAccount
        {
            Email = email,
            Password = "SecurePassword123!",
            AccountType = 2,
            GymSubscriptionType = 1,
            Gender = 0
        };

        // Act
        var accountGuid = await _authenticationService.RegisterAccount(insertAccount, CancellationToken.None);

        // Assert: Check Account
        var accounts = await _accountsRepository.FetchByConditionAsync(a => a.Id == accountGuid, CancellationToken.None);
        accounts.Should().ContainSingle(a => a.Email == email.ToLower());

        // Assert: Check Member
        var members = await _membersRepository.FetchByCondition(m => m.AccountGuid == accountGuid, CancellationToken.None);
        members
            .Should()
            .ContainSingle(m => m.Email == email.ToLower() 
                && m.AccountType == 2 
                && m.GymSubscriptionType == 1);
    }

    [Fact]
    public async Task GivenValidPasswordForExistingAccount_WhenChangingPassword_ThenPasswordIsChangedAndCanLoginWithNewPassword()
    {
        // Given
        var email = $"user{Guid.NewGuid():N}@test.com";
        var oldPassword = "oldPassword01";
        var insertAccount = new InsertAccount
        {
            Email = email,
            AccountType = (int)AccountType.Member,
            Gender = (int)Gender.Male,
            Password = oldPassword,
            GymSubscriptionType = (int)GymSubscriptionType.Monthly
        };
        var accountGuid = await _authenticationService.RegisterAccount(insertAccount, CancellationToken.None);
        accountGuid.Should().NotBe(Guid.Empty);
        var newPassword = "newPassword02";
        
        // When
        var passwordChanged = await _authenticationService.ChangePassword(email, oldPassword, newPassword, CancellationToken.None);
        passwordChanged.Should().BeTrue();
        _unitOfWork.DetachAll();
        
        // Then
        var authenticationRequestBody = new AuthenticationRequestBody
        {
            Password = newPassword,
            Username = email
        };
        var result = await _authenticationService.LoginAccount(authenticationRequestBody, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GivenNoAdminAccountExists_WhenSettingUpAdminAccount_ThenAdminAccountIsCreated()
    {
        // Given
        var email = $"admin{Guid.NewGuid():N}@test.com";
        var request = new SetupAdminAccount
        {
            Email = email,
            Password = "SecurePassword123!",
            TimeZone = "Europe/Zagreb"
        };

        // When
        var accountGuid = await _authenticationService.SetupAdminAccountAsync(request, CancellationToken.None);

        // Then
        accountGuid.Should().NotBe(Guid.Empty);
        var members = await _membersRepository.FetchByCondition(m => m.AccountGuid == accountGuid, CancellationToken.None);
        members.Should().ContainSingle(m => m.Email == email.ToLower() && m.AccountType == (int)AccountType.Admin);
        var hasAdmin = await _authenticationService.HasAdminAccountAsync(CancellationToken.None);
        hasAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task GivenAdminAccountAlreadyExists_WhenSettingUpAnotherAdminAccount_ThenAdminAccountAlreadyExistsExceptionIsThrown()
    {
        // Given - the server-side race/bypass guard: SetupAdminAccountAsync re-checks right
        // before creating the account, so this can only ever succeed once.
        var firstRequest = new SetupAdminAccount
        {
            Email = $"admin{Guid.NewGuid():N}@test.com",
            Password = "SecurePassword123!"
        };
        await _authenticationService.SetupAdminAccountAsync(firstRequest, CancellationToken.None);
        _unitOfWork.DetachAll();

        var secondRequest = new SetupAdminAccount
        {
            Email = $"admin{Guid.NewGuid():N}@test.com",
            Password = "AnotherPassword123!"
        };

        // When
        Func<Task> act = () => _authenticationService.SetupAdminAccountAsync(secondRequest, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<AdminAccountAlreadyExistsException>();
        var members = await _membersRepository.FetchByCondition(m => m.Email == secondRequest.Email.ToLower(), CancellationToken.None);
        members.Should().BeEmpty();
    }
}

