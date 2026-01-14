using FluentAssertions;
using GymCRM.IdentityAPI.Infrastructure.Implementation;
using GymCRM.IdentityAPI.Infrastructure.Interface;
using GymCRM.IdentityAPI.Models.Entities;
using GymCRM.IdentityAPI.Models.Implementation;
using GymCRM.IdentityAPI.Models.Interface;
using GymCRM.IdentityAPI.Models.Enums;

namespace GymCRM.IdentityAPI.Tests.Integration.Repositories;

public class TestMembersRepository : TestBase
{
    private IMembersRepository _membersRepository;
    private IAccountsRepository _accountsRepository;
    private IUnitOfWork _unitOfWork;

    public TestMembersRepository()
    {
        _membersRepository = new MembersRepository(_context);
        _accountsRepository = new AccountsRepository(_context);
        _unitOfWork = new UnitOfWork(_context);

        var accountsAndMembers = GenerateTestingAccounts();

        foreach (var testAccount in accountsAndMembers.testAccounts)
        {
            var testMember = accountsAndMembers.testMembers.Find(x => x.AccountGuid == testAccount.Id);

            if (testMember is null)
            {
                continue;
            }
            
            _accountsRepository.Insert(testAccount);
            _membersRepository.Insert(testMember);
            _unitOfWork.SaveAsync(CancellationToken.None).Wait();
        }
    }

    [Fact]
    public async Task GivenValidParameters_WhenGettingAllAccounts_ThenAllAccountsAreReturned()
    {
        // When
        var accounts = (await _accountsRepository.FetchAllAccountsAsync(CancellationToken.None)).ToList();
        
        // Then
        accounts.Should().NotBeNullOrEmpty();
        accounts.Should().HaveCount(4);
    }

    [Fact]
    public async Task GivenValidUpdatedMember_WhenUpdatingMember_ThenMemberIsProperlyUpdated()
    {
        // Given
        var account = new Account
        {
            Id = Guid.CreateVersion7(),
            Email = $"test.account{Guid.NewGuid()}@example.com",
            HashedPassword = "hashedpassword",
            HashSalt = "salty",
            DateCreated = DateTime.UtcNow
        };
        _accountsRepository.Insert(account);
        await _unitOfWork.SaveAsync(CancellationToken.None);

        var member = new Member
        {
            Id = Guid.CreateVersion7(),
            AccountGuid = account.Id,
            Email = account.Email.ToLower(),
            AccountType = 1,
            GymSubscriptionType = 0,
            Gender = 0,
            DateModified = account.DateCreated,
            TimeZone = TimeZoneInfo.Local.Id
        };
        _membersRepository.Insert(member);
        await _unitOfWork.SaveAsync(CancellationToken.None);
        
        var members = (await _membersRepository
            .FetchByCondition(m => m.AccountGuid == account.Id, CancellationToken.None))
            .ToList();
        members.Should().ContainSingle(m => m.Email == account.Email.ToLower());
        var existingMember = members.FirstOrDefault(x => x.AccountGuid == account.Id);
        existingMember.Should().NotBeNull();

        existingMember.AccountType = (int)AccountType.PersonalTrainer;
        existingMember.GymSubscriptionType = (int)GymSubscriptionType.Yearly;
        existingMember.DateModified = DateTime.UtcNow;
        existingMember.FirstName = "Testo";
        existingMember.LastName = "Testov";
        existingMember.Gender = (int)Gender.Female;
        _unitOfWork.Detach(existingMember);
        
        // When
        _membersRepository.Update(existingMember);
        await _unitOfWork.SaveAsync(CancellationToken.None);
        
        // Then
        var updatedMemberFromDb = (await _membersRepository
            .FetchByCondition(x => x.AccountGuid == existingMember.AccountGuid, CancellationToken.None))
            .FirstOrDefault();
        updatedMemberFromDb.Should().NotBeNull();
        updatedMemberFromDb.FirstName.Should().Be("Testo");
        updatedMemberFromDb.LastName.Should().Be("Testov");
        updatedMemberFromDb.Gender.Should().Be((int)Gender.Female);
        updatedMemberFromDb.DateModified.Should().BeCloseTo(existingMember.DateModified, TimeSpan.FromMilliseconds(1));
        updatedMemberFromDb.GymSubscriptionType.Should().Be((int)GymSubscriptionType.Yearly);
        updatedMemberFromDb.AccountType.Should().Be((int)AccountType.PersonalTrainer);
        updatedMemberFromDb.Email.Should().Be(existingMember.Email);
    }
    
    [Fact]
    public async Task GivenValidMember_WhenInsertingMember_ThenMemberIsProperlyInserted()
    {
        // Given
        var account = new Account
        {
            Id = Guid.CreateVersion7(),
            Email = $"test.account{Guid.NewGuid()}@example.com",
            HashedPassword = "hashedpassword",
            HashSalt = "salty",
            DateCreated = DateTime.UtcNow
        };
        _accountsRepository.Insert(account);
        await _unitOfWork.SaveAsync(CancellationToken.None);

        var member = new Member
        {
            Id = Guid.CreateVersion7(),
            AccountGuid = account.Id,
            Email = account.Email.ToLower(),
            AccountType = 1,
            GymSubscriptionType = 0,
            Gender = 0,
            DateModified = account.DateCreated,
            TimeZone = TimeZoneInfo.Local.Id
        };

        // When
        _membersRepository.Insert(member);
        await _unitOfWork.SaveAsync(CancellationToken.None);

        // Then
        var members = await _membersRepository.FetchByCondition(m => m.AccountGuid == account.Id, CancellationToken.None);
        members.Should().ContainSingle(m => m.Email == account.Email.ToLower());
    }

    private (List<Account> testAccounts, List<Member> testMembers) GenerateTestingAccounts()
    {
        var accountsForTests = new List<Account>();
        var membersForTests = new List<Member>();

        for (var i = 0; i < 4; i++)
        {
            var id = Guid.CreateVersion7();
            var account = new Account
            {
                Id = id,
                DateCreated = DateTime.UtcNow,
                Email = $"{id}@test.com",
                HashedPassword = $"testPassword{i}",
                HashSalt = "saltyTest",
            };

            var member = new Member
            {
                Id = Guid.CreateVersion7(),
                AccountGuid = account.Id,
                Email = account.Email.ToLower(),
                AccountType = (int)AccountType.Member,
                GymSubscriptionType = (int)GymSubscriptionType.Monthly,
                Gender = (int)Gender.Male,
                DateModified = DateTime.UtcNow,
                TimeZone = TimeZoneInfo.Local.Id
            };
            accountsForTests.Add(account);
            membersForTests.Add(member);
        }
        
        return (accountsForTests, membersForTests);
    }
}