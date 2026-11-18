using NullBattle.Matchmaking.Services;

namespace NullBattle.Matchmaking.Workers;

public class MatchmakingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MatchmakingWorker> _logger;

    public MatchmakingWorker(IServiceScopeFactory scopeFactory, ILogger<MatchmakingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var matchmakingService = scope.ServiceProvider.GetRequiredService<MatchmakingService>();
                await matchmakingService.FindMatchesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в MatchmakingWorker");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}