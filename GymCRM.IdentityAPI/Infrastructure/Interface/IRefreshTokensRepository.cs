using GymCRM.IdentityAPI.Models.Entities;

namespace GymCRM.IdentityAPI.Infrastructure.Interface;

public interface IRefreshTokensRepository : IDisposable
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken);
    Task<List<RefreshToken>> GetActiveTokensByAccountIdAsync(Guid accountId, CancellationToken cancellationToken);
    void Add(RefreshToken refreshToken);
    void Update(RefreshToken refreshToken);
    void BulkDelete(List<RefreshToken> refreshTokens);
    Task RevokeAllForAccountAsync(Guid accountId, string reason, CancellationToken cancellationToken);
    Task<List<RefreshToken>> GetExpiredTokensAsync(CancellationToken cancellationToken);
}