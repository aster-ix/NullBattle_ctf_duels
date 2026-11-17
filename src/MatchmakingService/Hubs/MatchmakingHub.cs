using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NullBattle.Matchmaking.DTOs;
using NullBattle.Matchmaking.Exceptions;
using NullBattle.Matchmaking.Models;
using NullBattle.Matchmaking.Services;

namespace NullBattle.Matchmaking.Hubs;

[Authorize]
public class MatchmakingHub : Hub
{
    private readonly MatchmakingService _matchmakingService;

    public MatchmakingHub(MatchmakingService matchmakingService)
    {
        _matchmakingService = matchmakingService;
    }

    public async Task<QueueStatusResponse> JoinQueue(Difficulty difficulty)
    {
        try
        {
            return await _matchmakingService.JoinQueueAsync(GetUserId(), Context.ConnectionId, difficulty);
        }
        catch (AppException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    public async Task LeaveQueue()
    {
        await _matchmakingService.LeaveQueueAsync(GetUserId(), Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _matchmakingService.LeaveQueueAsync(GetUserId(), Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private long GetUserId()
    {
        return long.Parse(Context.User!.FindFirst("sub")!.Value);
    }
}