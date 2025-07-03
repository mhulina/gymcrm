using GymCRM.MembershipAPI.Models.DTOs;
using GymCRM.MembershipAPI.Infrastructure.Interface;
using FluentAssertions;
using GymCRM.MembershipAPI.Infrastructure;
using GymCRM.MembershipAPI.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymCRM.MembershipAPI.Tests.Integration.Services;

public class AuthenticationServiceTests : IClassFixture<TestBase>, IAsyncLifetime
{
    private readonly TestBase _fixture;
    private IAuthenticationService _authenticationService;
    private IMembersRepository _membersRepository;
    private IAccountsRepository _accountsRepository;
    private AppDbContext _dbContext;

    public AuthenticationServiceTests(TestBase fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _authenticationService = _fixture.ServiceProvider.GetRequiredService<IAuthenticationService>();
        _membersRepository = _fixture.ServiceProvider.GetRequiredService<IMembersRepository>();
        _accountsRepository = _fixture.ServiceProvider.GetRequiredService<IAccountsRepository>();
        _dbContext = _fixture.ServiceProvider.GetRequiredService<AppDbContext>();

        // Clear Accounts and Members tables for test isolation
        await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Accounts\" RESTART IDENTITY CASCADE;");
        await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Members\" RESTART IDENTITY CASCADE;");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RegisterAccount_Should_Create_Account_And_Member()
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
        var accounts = await _accountsRepository.FetchByConditionAsync(a => a.Guid == accountGuid, CancellationToken.None);
        accounts.Should().ContainSingle(a => a.Email == email.ToLower());

        // Assert: Check Member
        var members = await _membersRepository.FetchByCondition(m => m.AccountGuid == accountGuid, CancellationToken.None);
        members.Should().ContainSingle(m => m.Email == email.ToLower() && m.AccountType == 2 && m.GymSubscriptionType == 1);
    }
}

