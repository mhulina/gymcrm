using GymCRM.IdentityAPI.Services.Interface;

namespace GymCRM.IdentityAPI.Services.Background;

public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(24));

    public RefreshTokenCleanupService(IServiceProvider serviceProvider, ILogger<RefreshTokenCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken)
            && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredTokensAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Refresh token cleanup tick failed");
            }
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