using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using GymCRM.IdentityAPI.Infrastructure.Interface;
using GymCRM.IdentityAPI.Models;
using GymCRM.IdentityAPI.Models.DTOs;
using GymCRM.IdentityAPI.Models.Enums;
using GymCRM.IdentityAPI.Models.Exceptions;
using GymCRM.IdentityAPI.Models.Interface;
using GymCRM.IdentityAPI.Services.Implementation;
using GymCRM.IdentityAPI.Services.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Serilog;
using Account = GymCRM.IdentityAPI.Models.Entities.Account;
using AuthenticationService = GymCRM.IdentityAPI.Services.Implementation.AuthenticationService;
using Member = GymCRM.IdentityAPI.Models.Entities.Member;
using RefreshToken = GymCRM.IdentityAPI.Models.Entities.RefreshToken;

namespace GymCRM.IdentityAPI.Tests.Unit;

public class TestAuthenticationService
{
    private const string TestSecretForKey = "RkhkaHF6PiwnNTBnWHInJGRVLDlYL1M/MXpdYGVaeyUiRi9mM3xlfWFVNy8kNThsWGdjJntQNDBOIlR7RWRL";
    private const string TestIssuer = "GymCRM.IdentityAPI.Test";
    private const string TestAudience = "GymCRM.Test";
    private const string ValidPassword = "CorrectPassword123!";

    [Theory]
    [InlineData(null, "Password123!")]
    [InlineData("", "Password123!")]
    [InlineData("user@test.com", null)]
    [InlineData("user@test.com", "")]
    public async Task GivenBlankCredentials_WhenRegisteringAccount_ThenArgumentExceptionIsThrown(string? email, string? password)
    {
        // Given
        var service = CreateAuthenticationService();
        var insertAccount = new InsertAccount { Email = email!, Password = password! };

        // When
        Func<Task> act = () => service.RegisterAccount(insertAccount);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenDuplicateEmail_WhenRegisteringAccount_ThenAccountAlreadyExistsExceptionIsThrown()
    {
        // Given
        var existingAccount = CreateAccount();
        var service = CreateAuthenticationService(accountsRepository: CreateAccountsRepositoryMock(existingAccount).Object);
        var insertAccount = new InsertAccount { Email = existingAccount.Email, Password = "Password123!" };

        // When
        Func<Task> act = () => service.RegisterAccount(insertAccount);

        // Then
        await act.Should().ThrowAsync<AccountAlreadyExistsException>();
    }

    [Fact]
    public async Task GivenValidInsertAccountWithNoDefaultsSpecified_WhenRegisteringAccount_ThenAccountAndMemberAreInsertedWithDefaults()
    {
        // Given
        var accountsRepositoryMock = CreateAccountsRepositoryMock();
        var membersRepositoryMock = new Mock<IMembersRepository>();
        var service = CreateAuthenticationService(
            accountsRepository: accountsRepositoryMock.Object,
            membersRepository: membersRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(saveResult: true).Object);
        var insertAccount = new InsertAccount { Email = "new@test.com", Password = "Password123!" };

        // When
        var result = await service.RegisterAccount(insertAccount);

        // Then
        result.Should().NotBe(Guid.Empty);
        accountsRepositoryMock.Verify(x => x.Insert(It.Is<Account>(a => a.Email == "new@test.com")), Times.Once);
        membersRepositoryMock.Verify(x => x.Insert(It.Is<Member>(m =>
            m.AccountType == (int)AccountType.Member
            && m.Gender == 0
            && m.TimeZone == TimeZoneInfo.Utc.Id)), Times.Once);
    }

    [Fact]
    public async Task GivenValidInsertAccount_WhenRegisteringAccount_ThenPasswordIsHashedInCurrentFormat()
    {
        // Given - new accounts must never be created with the legacy hand-rolled scheme, only
        // pre-existing ones (via the rehash-on-login/change path) should ever carry it.
        var accountsRepositoryMock = CreateAccountsRepositoryMock();
        var service = CreateAuthenticationService(
            accountsRepository: accountsRepositoryMock.Object,
            membersRepository: new Mock<IMembersRepository>().Object,
            unitOfWork: CreateUnitOfWorkMock(saveResult: true).Object);
        var insertAccount = new InsertAccount { Email = "new@test.com", Password = "Password123!" };

        // When
        await service.RegisterAccount(insertAccount);

        // Then
        accountsRepositoryMock.Verify(x => x.Insert(It.Is<Account>(a =>
            Convert.FromBase64String(a.HashedPassword)[0] == 1)), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GivenRepositoryResult_WhenCheckingHasAdminAccount_ThenResultIsPassedThrough(bool adminExists)
    {
        // Given
        var service = CreateAuthenticationService(membersRepository: CreateMembersRepositoryMock(adminExists).Object);

        // When
        var result = await service.HasAdminAccountAsync();

        // Then
        result.Should().Be(adminExists);
    }

    [Theory]
    [InlineData(null, "Password123!")]
    [InlineData("", "Password123!")]
    [InlineData("admin@test.com", null)]
    [InlineData("admin@test.com", "")]
    public async Task GivenBlankCredentials_WhenSettingUpAdminAccount_ThenArgumentExceptionIsThrown(string? email, string? password)
    {
        // Given
        var service = CreateAuthenticationService();
        var request = new SetupAdminAccount { Email = email!, Password = password! };

        // When
        Func<Task> act = () => service.SetupAdminAccountAsync(request);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenAdminAlreadyExists_WhenSettingUpAdminAccount_ThenAdminAccountAlreadyExistsExceptionIsThrownAndNothingIsInserted()
    {
        // Given - the server-side race/bypass guard: re-checks right before creating the account.
        var accountsRepositoryMock = CreateAccountsRepositoryMock();
        var service = CreateAuthenticationService(
            membersRepository: CreateMembersRepositoryMock(adminExists: true).Object,
            accountsRepository: accountsRepositoryMock.Object);
        var request = new SetupAdminAccount { Email = "admin@test.com", Password = "Password123!" };

        // When
        Func<Task> act = () => service.SetupAdminAccountAsync(request);

        // Then
        await act.Should().ThrowAsync<AdminAccountAlreadyExistsException>();
        accountsRepositoryMock.Verify(x => x.Insert(It.IsAny<Account>()), Times.Never);
    }

    [Fact]
    public async Task GivenNoAdminExists_WhenSettingUpAdminAccount_ThenAdminAccountIsCreated()
    {
        // Given
        var membersRepositoryMock = CreateMembersRepositoryMock(adminExists: false);
        var service = CreateAuthenticationService(
            membersRepository: membersRepositoryMock.Object,
            accountsRepository: CreateAccountsRepositoryMock().Object,
            unitOfWork: CreateUnitOfWorkMock(saveResult: true).Object);
        var request = new SetupAdminAccount { Email = "admin@test.com", Password = "Password123!", TimeZone = "Europe/Zagreb" };

        // When
        var result = await service.SetupAdminAccountAsync(request);

        // Then
        result.Should().NotBe(Guid.Empty);
        membersRepositoryMock.Verify(x => x.Insert(It.Is<Member>(m => m.AccountType == (int)AccountType.Admin)), Times.Once);
    }

    [Fact]
    public async Task GivenNonAdminCaller_WhenAdminCreatingAccount_ThenAccountAccessDeniedExceptionIsThrown()
    {
        // Given
        var caller = CreateAccount(accountType: AccountType.Member).Member;
        var membersRepositoryMock = new Mock<IMembersRepository>();
        membersRepositoryMock
            .Setup(x => x.FetchByCondition(It.IsAny<Expression<Func<Member, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Member> { caller });
        var service = CreateAuthenticationService(membersRepository: membersRepositoryMock.Object);
        var insertAccount = new InsertAccount { Email = "new@test.com", Password = "Password123!" };

        // When
        Func<Task> act = () => service.AdminCreateAccountAsync(insertAccount, caller.AccountGuid);

        // Then
        await act.Should().ThrowAsync<AccountAccessDeniedException>();
    }

    [Fact]
    public async Task GivenUnknownCaller_WhenAdminCreatingAccount_ThenAccountAccessDeniedExceptionIsThrown()
    {
        // Given
        var membersRepositoryMock = new Mock<IMembersRepository>();
        membersRepositoryMock
            .Setup(x => x.FetchByCondition(It.IsAny<Expression<Func<Member, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Member>());
        var service = CreateAuthenticationService(membersRepository: membersRepositoryMock.Object);
        var insertAccount = new InsertAccount { Email = "new@test.com", Password = "Password123!" };

        // When
        Func<Task> act = () => service.AdminCreateAccountAsync(insertAccount, Guid.NewGuid());

        // Then
        await act.Should().ThrowAsync<AccountAccessDeniedException>();
    }

    [Fact]
    public async Task GivenAdminCaller_WhenAdminCreatingAccount_ThenAccountIsCreatedWithMustChangePasswordSet()
    {
        // Given
        var adminMember = CreateAccount(accountType: AccountType.Admin).Member;
        var membersRepositoryMock = new Mock<IMembersRepository>();
        membersRepositoryMock
            .Setup(x => x.FetchByCondition(It.IsAny<Expression<Func<Member, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Member> { adminMember });
        var accountsRepositoryMock = CreateAccountsRepositoryMock();
        var service = CreateAuthenticationService(
            accountsRepository: accountsRepositoryMock.Object,
            membersRepository: membersRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(saveResult: true).Object);
        var insertAccount = new InsertAccount { Email = "newmember@test.com", Password = "Password123!" };

        // When
        var result = await service.AdminCreateAccountAsync(insertAccount, adminMember.AccountGuid);

        // Then
        result.Should().NotBe(Guid.Empty);
        accountsRepositoryMock.Verify(x => x.Insert(It.Is<Account>(a =>
            a.Email == "newmember@test.com" && a.MustChangePassword)), Times.Once);
        membersRepositoryMock.Verify(x => x.Insert(It.Is<Member>(m => m.Email == "newmember@test.com")), Times.Once);
    }

    [Theory]
    [InlineData(null, "pw")]
    [InlineData("", "pw")]
    [InlineData("user@test.com", null)]
    [InlineData("user@test.com", "")]
    public async Task GivenBlankCredentials_WhenLoggingIn_ThenArgumentExceptionIsThrown(string? username, string? password)
    {
        // Given
        var service = CreateAuthenticationService();

        // When
        Func<Task> act = () => service.LoginAccount(new AuthenticationRequestBody { Username = username!, Password = password! });

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenUnknownAccount_WhenLoggingIn_ThenAuthenticationExceptionIsThrown()
    {
        // Given
        var service = CreateAuthenticationService(accountsRepository: CreateAccountsRepositoryMock().Object);

        // When
        Func<Task> act = () => service.LoginAccount(new AuthenticationRequestBody { Username = "nobody@test.com", Password = "whatever" });

        // Then
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task GivenAccountCurrentlyLockedOut_WhenLoggingIn_ThenAuthenticationExceptionIsThrown()
    {
        // Given
        var account = CreateAccount(lockoutUntil: DateTime.UtcNow.AddMinutes(10));
        var service = CreateAuthenticationService(accountsRepository: CreateAccountsRepositoryMock(account).Object);

        // When
        Func<Task> act = () => service.LoginAccount(new AuthenticationRequestBody { Username = account.Email, Password = "irrelevant" });

        // Then
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task GivenWrongPassword_WhenLoggingIn_ThenAuthenticationExceptionIsThrownAndFailedAttemptsIncrement()
    {
        // Given
        var account = CreateAccount(failedLoginAttempts: 1);
        var accountsRepositoryMock = CreateAccountsRepositoryMock(account);
        var service = CreateAuthenticationService(
            accountsRepository: accountsRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(saveResult: true).Object);

        // When
        Func<Task> act = () => service.LoginAccount(new AuthenticationRequestBody { Username = account.Email, Password = "WrongPassword" });

        // Then
        await act.Should().ThrowAsync<AuthenticationException>();
        account.FailedLoginAttempts.Should().Be(2);
        account.LockoutUntil.Should().BeNull();
        accountsRepositoryMock.Verify(x => x.Update(account), Times.Once);
    }

    [Fact]
    public async Task GivenFifthConsecutiveWrongPassword_WhenLoggingIn_ThenAccountIsLockedOut()
    {
        // Given
        var account = CreateAccount(failedLoginAttempts: 4);
        var service = CreateAuthenticationService(
            accountsRepository: CreateAccountsRepositoryMock(account).Object,
            unitOfWork: CreateUnitOfWorkMock(saveResult: true).Object);

        // When
        Func<Task> act = () => service.LoginAccount(new AuthenticationRequestBody { Username = account.Email, Password = "WrongPassword" });

        // Then
        await act.Should().ThrowAsync<AuthenticationException>();
        account.FailedLoginAttempts.Should().Be(5);
        account.LockoutUntil.Should().NotBeNull();
        account.LockoutUntil!.Value.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GivenCorrectPassword_WhenLoggingIn_ThenAttemptsAndLockoutAreResetAndTokensAreReturned()
    {
        // Given
        var account = CreateAccount(failedLoginAttempts: 3);
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Token = "refresh-token-value",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        var refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        refreshTokenServiceMock.Setup(x => x.GenerateRefreshToken(account.Id)).Returns(refreshToken);
        refreshTokenServiceMock.Setup(x => x.SaveRefreshTokenAsync(refreshToken, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = CreateAuthenticationService(
            accountsRepository: CreateAccountsRepositoryMock(account).Object,
            unitOfWork: CreateUnitOfWorkMock(saveResult: true).Object,
            refreshTokenService: refreshTokenServiceMock.Object);

        // When
        var (accessToken, returnedRefreshToken, mustChangePassword) = await service.LoginAccount(
            new AuthenticationRequestBody { Username = account.Email, Password = ValidPassword });

        // Then
        accessToken.Should().NotBeNullOrWhiteSpace();
        returnedRefreshToken.Should().Be(refreshToken.Token);
        mustChangePassword.Should().BeFalse();
        account.FailedLoginAttempts.Should().Be(0);
        account.LockoutUntil.Should().BeNull();
    }

    [Fact]
    public async Task GivenLegacyHashedAccount_WhenLoggingInWithCorrectPassword_ThenAccountIsTransparentlyRehashedToNewFormat()
    {
        // Given - CreateAccount() produces a legacy (pre-PBKDF2) HMACSHA256 hash by default,
        // standing in for a real account that hasn't logged in since the hashing migration.
        var account = CreateAccount();
        var legacyHash = account.HashedPassword;
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Token = "refresh-token-value",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        var refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        refreshTokenServiceMock.Setup(x => x.GenerateRefreshToken(account.Id)).Returns(refreshToken);
        refreshTokenServiceMock.Setup(x => x.SaveRefreshTokenAsync(refreshToken, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var accountsRepositoryMock = CreateAccountsRepositoryMock(account);
        var service = CreateAuthenticationService(
            accountsRepository: accountsRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(saveResult: true).Object,
            refreshTokenService: refreshTokenServiceMock.Object);

        // When
        var result = await service.LoginAccount(new AuthenticationRequestBody { Username = account.Email, Password = ValidPassword });

        // Then - login still succeeds off the legacy hash, but the account is rehashed into the
        // new PBKDF2 format (marker byte 0x01) as a side effect of the very same Update() that
        // already resets FailedLoginAttempts - not a separate write.
        result.accessToken.Should().NotBeNullOrWhiteSpace();
        account.HashedPassword.Should().NotBe(legacyHash);
        Convert.FromBase64String(account.HashedPassword)[0].Should().Be(1);
        accountsRepositoryMock.Verify(x => x.Update(account), Times.Once);
    }

    [Fact]
    public async Task GivenAlreadyMigratedAccount_WhenLoggingInWithCorrectPassword_ThenHashIsNotChangedAgain()
    {
        // Given - a password hashed the current way (mirrors what CreateAccountWithHashedPassword
        // now produces), unlike CreateAccount()'s default legacy fixture.
        var account = CreateAccount();
        var newFormatHash = new PasswordHasher<Account>().HashPassword(account, account.HashPepper + ValidPassword);
        account.HashedPassword = newFormatHash;
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Token = "refresh-token-value",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        var refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        refreshTokenServiceMock.Setup(x => x.GenerateRefreshToken(account.Id)).Returns(refreshToken);
        refreshTokenServiceMock.Setup(x => x.SaveRefreshTokenAsync(refreshToken, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = CreateAuthenticationService(
            accountsRepository: CreateAccountsRepositoryMock(account).Object,
            unitOfWork: CreateUnitOfWorkMock(saveResult: true).Object,
            refreshTokenService: refreshTokenServiceMock.Object);

        // When
        var result = await service.LoginAccount(new AuthenticationRequestBody { Username = account.Email, Password = ValidPassword });

        // Then
        result.accessToken.Should().NotBeNullOrWhiteSpace();
        account.HashedPassword.Should().Be(newFormatHash);
    }

    [Fact]
    public async Task GivenWrongPassword_WhenLoggingInAgainstAlreadyMigratedAccount_ThenAuthenticationExceptionIsThrown()
    {
        // Given - regression coverage for the legacy-fallback path in VerifyPassword: a wrong
        // password against a NEW-format hash must not somehow also match via the legacy check.
        var account = CreateAccount();
        account.HashedPassword = new PasswordHasher<Account>().HashPassword(account, account.HashPepper + ValidPassword);
        var service = CreateAuthenticationService(
            accountsRepository: CreateAccountsRepositoryMock(account).Object,
            unitOfWork: CreateUnitOfWorkMock(saveResult: true).Object);

        // When
        Func<Task> act = () => service.LoginAccount(new AuthenticationRequestBody { Username = account.Email, Password = "WrongPassword" });

        // Then
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task GivenRefreshTokenFailsToSave_WhenLoggingIn_ThenAuthenticationFailureExceptionIsThrown()
    {
        // Given
        var account = CreateAccount();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Token = "refresh-token-value",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        var refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        refreshTokenServiceMock.Setup(x => x.GenerateRefreshToken(account.Id)).Returns(refreshToken);
        refreshTokenServiceMock.Setup(x => x.SaveRefreshTokenAsync(refreshToken, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var service = CreateAuthenticationService(
            accountsRepository: CreateAccountsRepositoryMock(account).Object,
            unitOfWork: CreateUnitOfWorkMock(saveResult: true).Object,
            refreshTokenService: refreshTokenServiceMock.Object);

        // When
        Func<Task> act = () => service.LoginAccount(new AuthenticationRequestBody { Username = account.Email, Password = ValidPassword });

        // Then
        await act.Should().ThrowAsync<AuthenticationFailureException>();
    }

    [Fact]
    public async Task GivenEmptyGuid_WhenDeletingAccount_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateAuthenticationService();

        // When
        Func<Task> act = () => service.DeleteAccount(Guid.Empty);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidGuid_WhenDeletingAccount_ThenAccountIsDeleted()
    {
        // Given
        var accountsRepositoryMock = new Mock<IAccountsRepository>();
        var service = CreateAuthenticationService(
            accountsRepository: accountsRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(saveResult: true).Object);
        var accountGuid = Guid.NewGuid();

        // When
        var result = await service.DeleteAccount(accountGuid);

        // Then
        result.Should().BeTrue();
        accountsRepositoryMock.Verify(x => x.Delete(It.Is<Account>(a => a.Id == accountGuid)), Times.Once);
    }

    [Fact]
    public async Task GivenNullEmail_WhenChangingPassword_ThenNullReferenceExceptionIsThrown()
    {
        // Given - ChangePassword calls email.Trim() before checking IsNullOrWhiteSpace, so a
        // literal null throws NullReferenceException rather than the documented ArgumentException.
        // Pinning this actual (surprising) behavior.
        var service = CreateAuthenticationService();

        // When
        Func<Task> act = () => service.ChangePassword(null!, "old", "new");

        // Then
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GivenBlankEmail_WhenChangingPassword_ThenArgumentExceptionIsThrown(string email)
    {
        // Given
        var service = CreateAuthenticationService();

        // When
        Func<Task> act = () => service.ChangePassword(email, "old", "new");

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenNullOldPassword_WhenChangingPassword_ThenNullReferenceExceptionIsThrown()
    {
        // Given - same .Trim()-before-null-check gap as the email argument above.
        var service = CreateAuthenticationService();

        // When
        Func<Task> act = () => service.ChangePassword("user@test.com", null!, "new");

        // Then
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    [Theory]
    [InlineData("", "new")]
    [InlineData("old", "")]
    public async Task GivenBlankPasswords_WhenChangingPassword_ThenArgumentExceptionIsThrown(string oldPassword, string newPassword)
    {
        // Given
        var service = CreateAuthenticationService();

        // When
        Func<Task> act = () => service.ChangePassword("user@test.com", oldPassword, newPassword);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenUnknownAccount_WhenChangingPassword_ThenAccountDoesntExistExceptionIsThrown()
    {
        // Given
        var service = CreateAuthenticationService(accountsRepository: CreateAccountsRepositoryMock().Object);

        // When
        Func<Task> act = () => service.ChangePassword("nobody@test.com", "old", "new");

        // Then
        await act.Should().ThrowAsync<AccountDoesntExistException>();
    }

    [Fact]
    public async Task GivenWrongOldPassword_WhenChangingPassword_ThenAuthenticationFailureExceptionIsThrown()
    {
        // Given
        var account = CreateAccount();
        var service = CreateAuthenticationService(accountsRepository: CreateAccountsRepositoryMock(account).Object);

        // When
        Func<Task> act = () => service.ChangePassword(account.Email, "WrongOldPassword", "NewPassword123!");

        // Then
        await act.Should().ThrowAsync<AuthenticationFailureException>();
    }

    [Fact]
    public async Task GivenCorrectOldPassword_WhenChangingPassword_ThenPasswordIsUpdatedAndMustChangePasswordIsCleared()
    {
        // Given - CreateAccount()'s legacy hash also exercises ChangePassword's old-password
        // check against the legacy fallback path (VerifyPassword), not just LoginAccount's.
        var account = CreateAccount(mustChangePassword: true);
        var originalHash = account.HashedPassword;
        var accountsRepositoryMock = CreateAccountsRepositoryMock(account);
        var service = CreateAuthenticationService(
            accountsRepository: accountsRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(saveResult: true).Object);

        // When
        var result = await service.ChangePassword(account.Email, ValidPassword, "NewPassword456!");

        // Then - a fresh hash is always written in the current (PBKDF2) format, regardless of
        // whether the old password verified via the legacy or current path.
        result.Should().BeTrue();
        account.HashedPassword.Should().NotBe(originalHash);
        Convert.FromBase64String(account.HashedPassword)[0].Should().Be(1);
        account.MustChangePassword.Should().BeFalse();
        accountsRepositoryMock.Verify(x => x.Update(account), Times.Once);
    }

    [Fact]
    public async Task GivenNewPasswordSameAsOld_WhenChangingPassword_ThenArgumentExceptionIsThrown()
    {
        // Given - forcing an actual change is the whole point of MustChangePassword, so
        // resubmitting the same password must not be accepted as satisfying it.
        var account = CreateAccount();
        var service = CreateAuthenticationService(accountsRepository: CreateAccountsRepositoryMock(account).Object);

        // When
        Func<Task> act = () => service.ChangePassword(account.Email, ValidPassword, ValidPassword);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GivenNullAccount_WhenGeneratingJwtToken_ThenArgumentNullExceptionIsThrown()
    {
        // Given
        var service = CreateAuthenticationService();

        // When
        Action act = () => service.GenerateJwtToken(null!);

        // Then
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GivenValidAccount_WhenGeneratingJwtToken_ThenTokenContainsExpectedClaims()
    {
        // Given
        var account = CreateAccount(accountType: AccountType.PersonalTrainer);
        var service = CreateAuthenticationService();

        // When
        var token = service.GenerateJwtToken(account);

        // Then
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == account.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == account.Email);
        jwt.Claims.Should().Contain(c => c.Type == "type" && c.Value == AccountType.PersonalTrainer.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "timezone" && c.Value == account.Member.TimeZone);
        jwt.Claims.Should().Contain(c => c.Type == "mustChangePassword" && c.Value == "False");
        jwt.Issuer.Should().Be(TestIssuer);
        jwt.Audiences.Should().Contain(TestAudience);
    }

    [Fact]
    public void GivenAccountWithMustChangePasswordTrue_WhenGeneratingJwtToken_ThenClaimReflectsTrue()
    {
        // Given
        var account = CreateAccount(mustChangePassword: true);
        var service = CreateAuthenticationService();

        // When
        var token = service.GenerateJwtToken(account);

        // Then
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "mustChangePassword" && c.Value == "True");
    }

    private static string HashPassword(string password, string salt, DateTime dateCreated)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(salt));

        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(salt + dateCreated + password)));
    }

    private static Account CreateAccount(
        string password = ValidPassword,
        int failedLoginAttempts = 0,
        DateTime? lockoutUntil = null,
        AccountType accountType = AccountType.Member,
        bool mustChangePassword = false)
    {
        var dateCreated = DateTime.UtcNow.AddDays(-30);
        const string salt = "test-salt";
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Email = "existing@test.com",
            DateCreated = dateCreated,
            HashPepper = salt,
            HashedPassword = HashPassword(password, salt, dateCreated),
            FailedLoginAttempts = failedLoginAttempts,
            LockoutUntil = lockoutUntil,
            MustChangePassword = mustChangePassword
        };
        account.Member = new Member
        {
            Id = Guid.NewGuid(),
            AccountGuid = account.Id,
            AccountType = (int)accountType,
            TimeZone = "Europe/Zagreb",
            Email = account.Email,
            Gender = 0,
            DateModified = DateTime.UtcNow
        };

        return account;
    }

    // Backs FetchByConditionAsync with an in-memory list and compiles/applies the predicate
    // expression against it - mirrors the same pattern used in TestMembersService.
    private static Mock<IAccountsRepository> CreateAccountsRepositoryMock(params Account[] accounts)
    {
        var backingList = accounts.ToList();
        var accountsRepositoryMock = new Mock<IAccountsRepository>();
        accountsRepositoryMock
            .Setup(x => x.FetchByConditionAsync(It.IsAny<Expression<Func<Account, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Account, bool>> expression, CancellationToken _) =>
                backingList.Where(expression.Compile()).ToList());

        return accountsRepositoryMock;
    }

    private static Mock<IMembersRepository> CreateMembersRepositoryMock(bool adminExists = false)
    {
        var membersRepositoryMock = new Mock<IMembersRepository>();
        membersRepositoryMock
            .Setup(x => x.AnyByAccountTypeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminExists);

        return membersRepositoryMock;
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock(bool saveResult)
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(saveResult);

        return unitOfWorkMock;
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:SecretForKey"] = TestSecretForKey,
                ["Authentication:Issuer"] = TestIssuer,
                ["Authentication:Audience"] = TestAudience
            })
            .Build();

    private static AuthenticationService CreateAuthenticationService(
        IUnitOfWork? unitOfWork = null,
        IAccountsRepository? accountsRepository = null,
        IMembersRepository? membersRepository = null,
        IRefreshTokenService? refreshTokenService = null,
        IConfiguration? configuration = null,
        IPasswordHasher<Account>? passwordHasher = null,
        ILogger? logger = null) =>
        new(
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            accountsRepository ?? Mock.Of<IAccountsRepository>(),
            membersRepository ?? Mock.Of<IMembersRepository>(),
            refreshTokenService ?? Mock.Of<IRefreshTokenService>(),
            configuration ?? CreateConfiguration(),
            // Real instance, not a mock - most tests exercise actual hash/verify behavior
            // (e.g. "password changed" assertions comparing hash bytes), same rationale as
            // CreateConfiguration() above returning a real, working config rather than a mock.
            passwordHasher ?? new PasswordHasher<Account>(),
            logger ?? Mock.Of<ILogger>());
}
