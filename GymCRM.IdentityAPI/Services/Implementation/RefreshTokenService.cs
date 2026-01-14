using System.Security.Cryptography;
using GymCRM.IdentityAPI.Infrastructure.Interface;
using GymCRM.IdentityAPI.Models.Entities;
using GymCRM.IdentityAPI.Models.Interface;
using GymCRM.IdentityAPI.Services.Interface;
using ILogger = Serilog.ILogger;

namespace GymCRM.IdentityAPI.Services.Implementation;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokensRepository _refreshTokensRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger _logger;
    
    private const int DefaultTokenLengthBytes = 64;
    private const int DefaultExpirationDays = 7;

    public RefreshTokenService(IRefreshTokensRepository refreshTokensRepository, IUnitOfWork unitOfWork, ILogger logger)
    {
        _refreshTokensRepository = refreshTokensRepository ?? throw new ArgumentNullException(nameof(refreshTokensRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public RefreshToken GenerateRefreshToken(Guid accountId)
    {
        if (accountId == Guid.Empty)
        {
            _logger.Error("{AccountId} is an invalid account ID", accountId);

            return null;
        }
        
        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(DefaultTokenLengthBytes)),
            ExpiresAt = DateTime.UtcNow.AddDays(DefaultExpirationDays), // 7 days
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };
        
        _logger.Information(
            "Generated new refresh token for account ID: {AccountId}, expires at {ExpiresAt}",
            accountId,
            refreshToken.ExpiresAt);

        return refreshToken;
    }

    public async Task<RefreshToken?> ValidateRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.Error("Refresh token is invalid: {RefreshToken}", token);
            
            return null;
        }

        try
        {
            var refreshToken = await _refreshTokensRepository.GetByTokenAsync(token, cancellationToken);

            if (refreshToken == null)
            {
                _logger.Warning("Refresh token not found in database");

                return refreshToken;
            }

            if (!refreshToken.IsActive)
            {
                _logger.Warning(
                    "Refresh token is inactive (revoked or expired) for account ID: {AccountId}",
                    refreshToken.AccountId);

                return null;
            }
        
            _logger.Information(
                "Successfully validated refresh token for account ID: {AccountId}",
                refreshToken.AccountId);
        
            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Error validating token");
            throw;
        }
    }

    public async Task<bool> RevokeRefreshTokenAsync(
        RefreshToken token, 
        string reason, 
        string? replacementToken = null,
        CancellationToken cancellationToken = default)
    {
        if (token is null)
        {
            _logger.Error(
                "{MethodName} called with {RefreshToken}", 
                nameof(RevokeRefreshTokenAsync), 
                token);
            throw new ArgumentNullException(nameof(token));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            _logger.Error("Revocation reason cannot be {RevocationReason}", reason);
            throw new ArgumentException("Revocation reason cannot be empty", nameof(reason));
        }

        try
        {
            token.IsRevoked = true;
            token.RevokedReason = reason;
            token.RevokedAt = DateTime.UtcNow;
            token.ReplacedByToken = replacementToken;

            _refreshTokensRepository.Update(token);
            var result = await _unitOfWork.SaveAsync(cancellationToken);

            if (result)
            {
                _logger.Information(
                    "Revoked refresh token for account {AccountId}, reason: {Reason}",
                    token.AccountId,
                    reason);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(
                ex, 
                "Error revoking refresh token for account {AccountId}",
                token.AccountId);
            throw;
        }
    }

    public async Task<bool> RevokeAllTokensForAccountAsync(Guid accountId, string reason, CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
        {
            _logger.Error("Revoke all tokens invoked with account ID: {AccountId}", accountId);
            throw new ArgumentException($"{accountId} is an invalid account ID", nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            _logger.Error("Revoke all tokens for account ID invoked with invalid reason: {Reason}", reason);
            throw new ArgumentException("Reason cannot be null or empty", nameof(reason));
        }

        await _refreshTokensRepository.RevokeAllForAccountAsync(accountId, reason, cancellationToken);
        var result = await _unitOfWork.SaveAsync(cancellationToken);

        if (result)
        {
            _logger.Information("Revoked all tokens for account ID: {AccountId}, reason: {Reason}", accountId, reason);
        }

        return result;
    }

    public async Task<bool> SaveRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        if (refreshToken is null)
        {
            _logger.Error("Refresh token is invalid: {RefreshToken}", refreshToken);
            throw new ArgumentNullException(nameof(refreshToken));
        }

        try
        {
            _refreshTokensRepository.Add(refreshToken);
            var result = await _unitOfWork.SaveAsync(cancellationToken);

            if (result)
            {
                _logger.Information("Saved refresh token for account ID: {AccountId}", refreshToken.AccountId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error saving refresh token for account ID: {AccountId}", refreshToken.AccountId);
            throw;
        }
    }

    public async Task<List<RefreshToken>> GetActiveTokensForAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
        {
            _logger.Error("Account ID is invalid: {AccountId}", accountId);
            throw new ArgumentException($"Invalid account ID: {accountId}", nameof(accountId));
        }

        try
        {
            var activeTokens = await _refreshTokensRepository.GetActiveTokensByAccountIdAsync(accountId, cancellationToken);
            
            _logger.Information(
                "Retrieved {ActiveTokensCount} active tokens for account ID: {AccountId}", 
                activeTokens.Count, 
                accountId);
            
            return activeTokens;
        }
        catch (Exception ex)
        {
            _logger.Error(ex,"Error retrieving active tokens for account ID: {AccountId}", accountId);
            throw;
        }
    }

    public async Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredTokens = await _refreshTokensRepository.GetExpiredTokensAsync(cancellationToken);

            if (expiredTokens.Count == 0)
            {
                _logger.Information("No expired tokens found to clean up");
                
                return 0;
            }
            
            _refreshTokensRepository.BulkDelete(expiredTokens);
            await _unitOfWork.SaveAsync(cancellationToken);
            
            _logger.Information("Cleaned up {ExpiredTokensCount} expired tokens", expiredTokens.Count);
            
            return expiredTokens.Count;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error cleaning up expired tokens");
            throw;
        }
    }
}