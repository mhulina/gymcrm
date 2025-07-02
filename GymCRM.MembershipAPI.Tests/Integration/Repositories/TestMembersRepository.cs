using FluentAssertions;
using GymCRM.MembershipAPI.Infrastructure;
using GymCRM.MembershipAPI.Infrastructure.Entities;
using GymCRM.MembershipAPI.Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymCRM.MembershipAPI.Tests.Integration.Repositories;

public class TestMembersRepository : IClassFixture<TestDatabaseFixture>, IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private IMembersRepository _membersRepository;
    private IAccountsRepository _accountsRepository;
    private AppDbContext _dbContext;

    public TestMembersRepository(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }
    
    public async Task InitializeAsync()
    {
        _membersRepository = _fixture.ServiceProvider.GetRequiredService<IMembersRepository>();
        _accountsRepository = _fixture.ServiceProvider.GetRequiredService<IAccountsRepository>();
        _dbContext = _fixture.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Clean the Members table before each test for isolation
        await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Members\" RESTART IDENTITY CASCADE;");
        await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Accounts\" RESTART IDENTITY CASCADE;");
    }

    public Task DisposeAsync() => Task.CompletedTask;
    
    [Fact]
    public async Task InsertMember_WithValidAccount_ShouldSucceed()
    {
        // Arrange: Insert a valid account first
        var account = new Account
        {
            Guid = Guid.NewGuid(),
            Email = "test.account@example.com",
            HashedPassword = "hashedpassword", // or however you store it
            HashSalt = "salty",
            DateCreated = DateTime.UtcNow
            // fill required fields as per your entity
        };
        _accountsRepository.Insert(account);
        await _dbContext.SaveChangesAsync();

        // Now create a member linked to that account
        var member = new Member
        {
            AccountGuid = account.Guid,
            Email = account.Email.ToLower(),
            AccountType = 1,
            GymSubscriptionType = 0,
            Gender = 0,
            DateModified = account.DateCreated
            // fill required fields as needed
        };

        // Act
        _membersRepository.Insert(member);
        await _dbContext.SaveChangesAsync();

        // Assert
        var members = await _membersRepository.FetchByCondition(m => m.AccountGuid == account.Guid, CancellationToken.None);
        members.Should().ContainSingle(m => m.Email == account.Email.ToLower());
    }
}