using GymCRM.IdentityAPI.Infrastructure.Interface;
using GymCRM.IdentityAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.IdentityAPI.Infrastructure.Implementation;

public class RefreshTokensRepository : IRefreshTokensRepository
{
    private readonly IdentityDbContext _context;

    public RefreshTokensRepository(IdentityDbContext context)
    {
        _context = context;
    }
    
    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken)
    {
        var result = await _context.RefreshTokens
            .Include(x => x.Account)
            .ThenInclude(x => x.Member)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);

        return result;
    }

    public async Task<List<RefreshToken>> GetActiveTokensByAccountIdAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var result = await _context.RefreshTokens
            .Where(x => x.AccountId == accountId
                && x.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return result;
    }

    public void Add(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
    }

    public void Update(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
    }

    public void BulkDelete(List<RefreshToken> refreshTokens)
    {
        _context.RefreshTokens.RemoveRange(refreshTokens);
    }

    public async Task RevokeAllForAccountAsync(Guid accountId, string reason, CancellationToken cancellationToken)
    {
        var tokens = await _context.RefreshTokens
            .Where(x => x.AccountId == accountId
                && !x.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedReason = reason;
            token.RevokedAt = DateTime.UtcNow;
        }
    }

    public async Task<List<RefreshToken>> GetExpiredTokensAsync(CancellationToken cancellationToken)
    {
        var result = await _context.RefreshTokens
            .Where(x => x.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);
        
        return result;
    }
    
    private bool _disposed = false;
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
        }
        _disposed = true;
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}