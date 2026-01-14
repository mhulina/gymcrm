using GymCRM.IdentityAPI.Services.Interface;

namespace GymCRM.IdentityAPI.Services.Background;

public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(24));
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken)
            && !stoppingToken.IsCancellationRequested)
        {
            await CleanupExpiredTokensAsync(stoppingToken);
        }
    }

    private async Task CleanupExpiredTokensAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var refreshTokenService = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();
        
        var deletedCount = await refreshTokenService.CleanupExpiredTokensAsync(stoppingToken);
        
        _logger.LogInformation("Deleted {DeletedRefreshTokensCount} refresh tokens", deletedCount);
    }
}