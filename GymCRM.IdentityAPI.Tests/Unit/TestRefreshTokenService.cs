using FluentAssertions;
using GymCRM.IdentityAPI.Infrastructure.Interface;
using GymCRM.IdentityAPI.Models.Entities;
using GymCRM.IdentityAPI.Models.Interface;
using GymCRM.IdentityAPI.Services.Implementation;
using Moq;
using Serilog;

namespace GymCRM.IdentityAPI.Tests.Unit;

public class TestRefreshTokenService
{
    [Fact]
    public void GivenEmptyAccountId_WhenGeneratingRefreshToken_ThenNullIsReturned()
    {
        // Given - RefreshTokenService.GenerateRefreshToken logs and returns null for an empty
        // guid rather than throwing - pinning this actual behavior.
        var service = CreateRefreshTokenService();

        // When
        var result = service.GenerateRefreshToken(Guid.Empty);

        // Then
        result.Should().BeNull();
    }

    [Fact]
    public void GivenValidAccountId_WhenGeneratingRefreshToken_ThenTokenIsGenerated()
    {
        // Given
        var service = CreateRefreshTokenService();
        var accountId = Guid.NewGuid();

        // When
        var result = service.GenerateRefreshToken(accountId);

        // Then
        result.Should().NotBeNull();
        result.AccountId.Should().Be(accountId);
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.IsRevoked.Should().BeFalse();
        result.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GivenBlankToken_WhenValidatingRefreshToken_ThenNullIsReturned()
    {
        // Given
        var service = CreateRefreshTokenService();

        // When
        var result = await service.ValidateRefreshTokenAsync("");

        // Then
        result.Should().BeNull();
    }

    [Fact]
    public async Task GivenTokenNotFound_WhenValidatingRefreshToken_ThenNullIsReturned()
    {
        // Given
        var repositoryMock = new Mock<IRefreshTokensRepository>();
        repositoryMock.Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((RefreshToken?)null);
        var service = CreateRefreshTokenService(repository: repositoryMock.Object);

        // When
        var result = await service.ValidateRefreshTokenAsync("unknown-token");

        // Then
        result.Should().BeNull();
    }

    [Fact]
    public async Task GivenInactiveToken_WhenValidatingRefreshToken_ThenNullIsReturned()
    {
        // Given
        var revokedToken = CreateToken(isRevoked: true);
        var repositoryMock = new Mock<IRefreshTokensRepository>();
        repositoryMock.Setup(x => x.GetByTokenAsync(revokedToken.Token, It.IsAny<CancellationToken>())).ReturnsAsync(revokedToken);
        var service = CreateRefreshTokenService(repository: repositoryMock.Object);

        // When
        var result = await service.ValidateRefreshTokenAsync(revokedToken.Token);

        // Then
        result.Should().BeNull();
    }

    [Fact]
    public async Task GivenActiveToken_WhenValidatingRefreshToken_ThenTokenIsReturned()
    {
        // Given
        var activeToken = CreateToken();
        var repositoryMock = new Mock<IRefreshTokensRepository>();
        repositoryMock.Setup(x => x.GetByTokenAsync(activeToken.Token, It.IsAny<CancellationToken>())).ReturnsAsync(activeToken);
        var service = CreateRefreshTokenService(repository: repositoryMock.Object);

        // When
        var result = await service.ValidateRefreshTokenAsync(activeToken.Token);

        // Then
        result.Should().Be(activeToken);
    }

    [Fact]
    public async Task GivenNullToken_WhenRevokingRefreshToken_ThenArgumentNullExceptionIsThrown()
    {
        // Given
        var service = CreateRefreshTokenService();

        // When
        Func<Task> act = () => service.RevokeRefreshTokenAsync(null!, "reason");

        // Then
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenBlankReason_WhenRevokingRefreshToken_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateRefreshTokenService();
        var token = CreateToken();

        // When
        Func<Task> act = () => service.RevokeRefreshTokenAsync(token, "");

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidTokenAndReason_WhenRevokingRefreshToken_ThenTokenIsMarkedRevoked()
    {
        // Given
        var token = CreateToken();
        var repositoryMock = new Mock<IRefreshTokensRepository>();
        var service = CreateRefreshTokenService(repository: repositoryMock.Object, unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When
        var result = await service.RevokeRefreshTokenAsync(token, "Password changed", "new-token-value");

        // Then
        result.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
        token.RevokedReason.Should().Be("Password changed");
        token.ReplacedByToken.Should().Be("new-token-value");
        token.RevokedAt.Should().NotBeNull();
        repositoryMock.Verify(x => x.Update(token), Times.Once);
    }

    [Fact]
    public async Task GivenEmptyAccountId_WhenRevokingAllTokensForAccount_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateRefreshTokenService();

        // When
        Func<Task> act = () => service.RevokeAllTokensForAccountAsync(Guid.Empty, "reason");

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenBlankReason_WhenRevokingAllTokensForAccount_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateRefreshTokenService();

        // When
        Func<Task> act = () => service.RevokeAllTokensForAccountAsync(Guid.NewGuid(), "");

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidAccountIdAndReason_WhenRevokingAllTokensForAccount_ThenRepositoryIsCalled()
    {
        // Given
        var accountId = Guid.NewGuid();
        var repositoryMock = new Mock<IRefreshTokensRepository>();
        var service = CreateRefreshTokenService(repository: repositoryMock.Object, unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When
        var result = await service.RevokeAllTokensForAccountAsync(accountId, "Security breach");

        // Then
        result.Should().BeTrue();
        repositoryMock.Verify(x => x.RevokeAllForAccountAsync(accountId, "Security breach", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenNullToken_WhenSavingRefreshToken_ThenArgumentNullExceptionIsThrown()
    {
        // Given
        var service = CreateRefreshTokenService();

        // When
        Func<Task> act = () => service.SaveRefreshTokenAsync(null!);

        // Then
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenValidToken_WhenSavingRefreshToken_ThenTokenIsAdded()
    {
        // Given
        var token = CreateToken();
        var repositoryMock = new Mock<IRefreshTokensRepository>();
        var service = CreateRefreshTokenService(repository: repositoryMock.Object, unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When
        var result = await service.SaveRefreshTokenAsync(token);

        // Then
        result.Should().BeTrue();
        repositoryMock.Verify(x => x.Add(token), Times.Once);
    }

    [Fact]
    public async Task GivenEmptyAccountId_WhenGettingActiveTokensForAccount_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateRefreshTokenService();

        // When
        Func<Task> act = () => service.GetActiveTokensForAccountAsync(Guid.Empty);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidAccountId_WhenGettingActiveTokensForAccount_ThenRepositoryResultIsReturned()
    {
        // Given
        var accountId = Guid.NewGuid();
        var activeTokens = new List<RefreshToken> { CreateToken() };
        var repositoryMock = new Mock<IRefreshTokensRepository>();
        repositoryMock.Setup(x => x.GetActiveTokensByAccountIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(activeTokens);
        var service = CreateRefreshTokenService(repository: repositoryMock.Object);

        // When
        var result = await service.GetActiveTokensForAccountAsync(accountId);

        // Then
        result.Should().BeEquivalentTo(activeTokens);
    }

    [Fact]
    public async Task GivenNoExpiredTokens_WhenCleaningUpExpiredTokens_ThenZeroIsReturnedAndNothingIsDeleted()
    {
        // Given
        var repositoryMock = new Mock<IRefreshTokensRepository>();
        repositoryMock.Setup(x => x.GetExpiredTokensAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<RefreshToken>());
        var service = CreateRefreshTokenService(repository: repositoryMock.Object);

        // When
        var result = await service.CleanupExpiredTokensAsync();

        // Then
        result.Should().Be(0);
        repositoryMock.Verify(x => x.BulkDelete(It.IsAny<List<RefreshToken>>()), Times.Never);
    }

    [Fact]
    public async Task GivenExpiredTokensExist_WhenCleaningUpExpiredTokens_ThenTheyAreDeletedAndCountIsReturned()
    {
        // Given
        var expiredTokens = new List<RefreshToken> { CreateToken(), CreateToken() };
        var repositoryMock = new Mock<IRefreshTokensRepository>();
        repositoryMock.Setup(x => x.GetExpiredTokensAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expiredTokens);
        var service = CreateRefreshTokenService(repository: repositoryMock.Object, unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When
        var result = await service.CleanupExpiredTokensAsync();

        // Then
        result.Should().Be(2);
        repositoryMock.Verify(x => x.BulkDelete(expiredTokens), Times.Once);
    }

    private static RefreshToken CreateToken(bool isRevoked = false) => new()
    {
        Id = Guid.NewGuid(),
        AccountId = Guid.NewGuid(),
        Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        CreatedAt = DateTime.UtcNow,
        IsRevoked = isRevoked
    };

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock(bool saveResult)
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(saveResult);

        return unitOfWorkMock;
    }

    private static RefreshTokenService CreateRefreshTokenService(
        IRefreshTokensRepository? repository = null,
        IUnitOfWork? unitOfWork = null,
        ILogger? logger = null) =>
        new(
            repository ?? Mock.Of<IRefreshTokensRepository>(),
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            logger ?? Mock.Of<ILogger>());
}
