using Microsoft.AspNetCore.SignalR;
using NullBattle.Duels.Hubs;
using NullBattle.Duels.Models;
using NullBattle.Duels.Services;

namespace NullBattle.Duels.Workers;

public class DuelTimeoutWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<DuelHub> _hub;
    private readonly ILogger<DuelTimeoutWorker> _logger;

    public DuelTimeoutWorker(
        IServiceScopeFactory scopeFactory,
        IHubContext<DuelHub> hub,
        ILogger<DuelTimeoutWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var duelService = scope.ServiceProvider.GetRequiredService<DuelService>();

                var finished = await duelService.FinishExpiredDuelsAsync();

                foreach (var duel in finished)
                {
                    var eventName = duel.Status == DuelStatus.CANCELLED ? "DuelCancelled" : "DuelFinished";
                    var group = DuelHub.GetGroupName(duel.Id);
                    await _hub.Clients.Group(group).SendAsync(eventName, duel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в DuelTimeoutWorker");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}