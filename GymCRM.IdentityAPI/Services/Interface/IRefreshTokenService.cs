using GymCRM.IdentityAPI.Models.Entities;

namespace GymCRM.IdentityAPI.Services.Interface;

public interface IRefreshTokenService
{
    /// <summary>
    /// Generates a new refresh token for the specified account.
    /// </summary>
    /// <param name="accountId">The unique identifier of the account.</param>
    /// <returns>A newly generated <see cref="RefreshToken"/> entity.</returns>
    RefreshToken GenerateRefreshToken(Guid accountId);
    
    /// <summary>
    /// Validates a refresh token and returns it if active.
    /// </summary>
    /// <param name="token">The refresh token string to validate.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the <see cref="RefreshToken"/> if valid and active; otherwise, null.
    /// </returns>
    Task<RefreshToken?> ValidateRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Revokes a refresh token and optionally replaces it with a new one (token rotation).
    /// </summary>
    /// <param name="token">The refresh token to revoke.</param>
    /// <param name="reason">The reason for revocation.</param>
    /// <param name="replacementToken">Optional replacement token string for token rotation.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing true if revocation was successful; otherwise, false.
    /// </returns>
    Task<bool> RevokeRefreshTokenAsync(
        RefreshToken token, 
        string reason, 
        string? replacementToken = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Revokes all active refresh tokens for the specified account.
    /// </summary>
    /// <param name="accountId">The unique identifier of the account.</param>
    /// <param name="reason">The reason for revoking all tokens (e.g., "Password changed", "Security breach").</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing true if revocation was successful; otherwise, false.
    /// </returns>
    Task<bool> RevokeAllTokensForAccountAsync(
        Guid accountId, 
        string reason, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves a new refresh token to the database.
    /// </summary>
    /// <param name="refreshToken">The refresh token to save.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing true if save was successful; otherwise, false.
    /// </returns>
    Task<bool> SaveRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all active refresh tokens for the specified account.
    /// </summary>
    /// <param name="accountId">The unique identifier of the account.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing a list of active <see cref="RefreshToken"/> entities.
    /// </returns>
    Task<List<RefreshToken>> GetActiveTokensForAccountAsync(
        Guid accountId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cleans up expired refresh tokens from the database.
    /// Should be called periodically (e.g., via background service).
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the number of tokens cleaned up.
    /// </returns>
    Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}