using GymCRM.IdentityAPI.Models.DTOs;
using GymCRM.IdentityAPI.Models.Interface;
using FluentAssertions;
using GymCRM.IdentityAPI.Models;
using GymCRM.IdentityAPI.Models.Enums;
using GymCRM.IdentityAPI.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymCRM.IdentityAPI.Tests.Integration.Services;

public class AuthenticationServiceTests : TestBase
{
    private IAuthenticationService _authenticationService;
    private IMembersRepository _membersRepository;
    private IAccountsRepository _accountsRepository;

    public AuthenticationServiceTests()
    {
        _authenticationService = ServiceProvider.GetRequiredService<IAuthenticationService>();
        _membersRepository = ServiceProvider.GetRequiredService<IMembersRepository>();
        _accountsRepository = ServiceProvider.GetRequiredService<IAccountsRepository>();

        // Clear Accounts and Members tables for test isolation
        _context.Database
            .ExecuteSqlRawAsync("TRUNCATE TABLE \"Accounts\" RESTART IDENTITY CASCADE;")
            .GetAwaiter()
            .GetResult();
        _context.Database
            .ExecuteSqlRawAsync("TRUNCATE TABLE \"Members\" RESTART IDENTITY CASCADE;")
            .GetAwaiter()
            .GetResult();

        ClearDatabase();
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
        using (var scope = ServiceProvider.CreateScope())
        {
            var passwordChanged = await 
                scope.ServiceProvider
                    .GetRequiredService<IAuthenticationService>()
                    .ChangePassword(email, oldPassword, newPassword, CancellationToken.None);
            passwordChanged.Should().BeTrue();
        }
        
        // Then
        var authenticationRequestBody = new AuthenticationRequestBody
        {
            Password = newPassword,
            Username = email
        };
        var result = await _authenticationService.LoginAccount(authenticationRequestBody, CancellationToken.None);
        
        result.Should().NotBeNull();
    }
}

