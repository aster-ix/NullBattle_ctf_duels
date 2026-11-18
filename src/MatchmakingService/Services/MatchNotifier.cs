using Microsoft.AspNetCore.SignalR;
using NullBattle.Matchmaking.Hubs;

namespace NullBattle.Matchmaking.Services;

public class MatchNotifier
{
    private readonly IHubContext<MatchmakingHub> _hub;

    public MatchNotifier(IHubContext<MatchmakingHub> hub)
    {
        _hub = hub;
    }

    public async Task NotifyMatchFoundAsync(string connectionId, long duelId)
    {
        await _hub.Clients.Client(connectionId).SendAsync("MatchFound", new { duelId });
    }

    public async Task NotifyMatchFailedAsync(string connectionId)
    {
        await _hub.Clients.Client(connectionId).SendAsync("MatchFailed");
    }
}