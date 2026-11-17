using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NullBattle.Duels.Exceptions;
using NullBattle.Duels.Services;

namespace NullBattle.Duels.Hubs;

[Authorize]
public class DuelHub : Hub
{
    private readonly DuelService _duelService;

    public DuelHub(DuelService duelService)
    {
        _duelService = duelService;
    }

    public async Task JoinDuel(long duelId)
    {
        await Run(async () =>
        {
            var duel = await _duelService.GetDuelAsync(duelId, GetUserId());
            await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(duelId));
            await Clients.Caller.SendAsync("DuelState", duel);
        });
    }

    public async Task<bool> SubmitFlag(long duelId, string flag)
    {
        var isCorrect = false;

        await Run(async () =>
        {
            var userId = GetUserId();
            var result = await _duelService.SubmitFlagAsync(duelId, userId, flag);
            isCorrect = result.IsCorrect;

            var group = GetGroupName(duelId);
            if (result.IsCorrect)
            {
                await Clients.Group(group).SendAsync("DuelFinished", result.Duel);
            }
            else
            {
                await Clients.OthersInGroup(group).SendAsync("OpponentWrongFlag", new
                {
                    userId = userId,
                    attemptNumber = result.AttemptNumber
                });
            }
        });

        return isCorrect;
    }

    public async Task BanCategory(long duelId, long categoryId)
    {
        await Run(async () =>
        {
            var result = await _duelService.BanCategoryAsync(duelId, GetUserId(), categoryId);
            var group = Clients.Group(GetGroupName(duelId));

            await group.SendAsync("CategoryBanned", result.Duel);
            if (result.Started)
            {
                await group.SendAsync("DuelStarted", result.Duel);
            }
        });
    }

    private long GetUserId()
    {
        return long.Parse(Context.User!.FindFirst("sub")!.Value);
    }

    public static string GetGroupName(long duelId)
    {
        return "duel-" + duelId;
    }

    private static async Task Run(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (AppException ex)
        {
            throw new HubException(ex.Message);
        }
    }
}