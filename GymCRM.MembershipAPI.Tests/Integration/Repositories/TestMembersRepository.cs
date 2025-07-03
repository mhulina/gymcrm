using FluentAssertions;
using FluentAssertions.Extensions;
using GymCRM.MembershipAPI.Infrastructure;
using GymCRM.MembershipAPI.Infrastructure.Entities;
using GymCRM.MembershipAPI.Infrastructure.Implementation;
using GymCRM.MembershipAPI.Infrastructure.Interface;
using GymCRM.MembershipAPI.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace GymCRM.MembershipAPI.Tests.Integration.Repositories;

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
    }

    [Fact]
    public async Task GivenValidUpdatedMember_WhenUpdatingMember_ThenMemberIsProperlyUpdated()
    {
        // Given
        var account = new Account
        {
            Guid = Guid.NewGuid(),
            Email = $"test.account{Guid.NewGuid()}@example.com",
            HashedPassword = "hashedpassword",
            HashSalt = "salty",
            DateCreated = DateTime.UtcNow
        };
        _accountsRepository.Insert(account);
        await _unitOfWork.SaveAsync(CancellationToken.None);

        var member = new Member
        {
            AccountGuid = account.Guid,
            Email = account.Email.ToLower(),
            AccountType = 1,
            GymSubscriptionType = 0,
            Gender = 0,
            DateModified = account.DateCreated
        };
        _membersRepository.Insert(member);
        await _unitOfWork.SaveAsync(CancellationToken.None);
        
        var members = (await _membersRepository
            .FetchByCondition(m => m.AccountGuid == account.Guid, CancellationToken.None))
            .ToList();
        members.Should().ContainSingle(m => m.Email == account.Email.ToLower());
        var existingMember = members.FirstOrDefault(x => x.AccountGuid == account.Guid);
        existingMember.Should().NotBeNull();

        var updatedMember = new Member
        {
            AccountGuid = existingMember.AccountGuid,
            Email = existingMember.Email,
            AccountType = (int)AccountType.PersonalTrainer,
            GymSubscriptionType = (int)GymSubscriptionType.Yearly,
            DateModified = DateTime.UtcNow,
            FirstName = "Testo",
            LastName = "Testov",
            Gender = (int)Gender.Female
        };
        
        // When
        _membersRepository.Update(updatedMember);
        await _unitOfWork.SaveAsync(CancellationToken.None);
        
        // Then
        var updatedMemberFromDb = (await _membersRepository
            .FetchByCondition(x => x.AccountGuid == updatedMember.AccountGuid, CancellationToken.None))
            .FirstOrDefault();
        updatedMemberFromDb.Should().NotBeNull();
        updatedMemberFromDb.FirstName.Should().Be("Testo");
        updatedMemberFromDb.LastName.Should().Be("Testov");
        updatedMemberFromDb.Gender.Should().Be((int)Gender.Female);
        updatedMemberFromDb.DateModified.Should().BeCloseTo(updatedMember.DateModified, TimeSpan.FromMilliseconds(1));
        updatedMemberFromDb.GymSubscriptionType.Should().Be((int)GymSubscriptionType.Yearly);
        updatedMemberFromDb.AccountType.Should().Be((int)AccountType.PersonalTrainer);
        updatedMemberFromDb.Email.Should().Be(updatedMember.Email);
    }
    
    [Fact]
    public async Task GivenValidMember_WhenInsertingMember_ThenMemberIsProperlyInserted()
    {
        // Given
        var account = new Account
        {
            Guid = Guid.NewGuid(),
            Email = $"test.account{Guid.NewGuid()}@example.com",
            HashedPassword = "hashedpassword",
            HashSalt = "salty",
            DateCreated = DateTime.UtcNow
        };
        _accountsRepository.Insert(account);
        await _unitOfWork.SaveAsync(CancellationToken.None);

        var member = new Member
        {
            AccountGuid = account.Guid,
            Email = account.Email.ToLower(),
            AccountType = 1,
            GymSubscriptionType = 0,
            Gender = 0,
            DateModified = account.DateCreated
        };

        // When
        _membersRepository.Insert(member);
        await _unitOfWork.SaveAsync(CancellationToken.None);

        // Then
        var members = await _membersRepository.FetchByCondition(m => m.AccountGuid == account.Guid, CancellationToken.None);
        members.Should().ContainSingle(m => m.Email == account.Email.ToLower());
    }
}