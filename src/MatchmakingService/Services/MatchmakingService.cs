using NullBattle.Matchmaking.Clients;
using NullBattle.Matchmaking.DTOs;
using NullBattle.Matchmaking.Exceptions;
using NullBattle.Matchmaking.Models;
using NullBattle.Matchmaking.Storage;

namespace NullBattle.Matchmaking.Services;

public class MatchmakingService
{
    private static readonly SemaphoreSlim QueueLock = new SemaphoreSlim(1, 1);

    private readonly QueueStore _queueStore;
    private readonly RatingServiceClient _ratingClient;

    public MatchmakingService(QueueStore queueStore, RatingServiceClient ratingClient)
    {
        _queueStore = queueStore;
        _ratingClient = ratingClient;
    }

    public async Task<QueueStatusResponse> JoinQueueAsync(long userId, string connectionId, Difficulty difficulty)
    {
        if (!Enum.IsDefined(difficulty))
        {
            throw new AppException(400, "Недопустимая сложность");
        }

        var rating = await _ratingClient.GetRatingAsync(userId);

        await QueueLock.WaitAsync();
        try
        {
            if (await _queueStore.GetAsync(userId) != null)
            {
                throw new AppException(409, "Вы уже в очереди");
            }

            var entry = new QueueEntry
            {
                UserId = userId,
                Rating = rating,
                Difficulty = difficulty,
                ConnectionId = connectionId,
                JoinedAt = DateTime.UtcNow
            };
            await _queueStore.AddAsync(entry);

            return ToStatus(entry);
        }
        finally
        {
            QueueLock.Release();
        }
    }

    public async Task LeaveQueueAsync(long userId, string connectionId)
    {
        await QueueLock.WaitAsync();
        try
        {
            var entry = await _queueStore.GetAsync(userId);
            if (entry != null && entry.ConnectionId == connectionId)
            {
                await _queueStore.RemoveAsync(userId);
            }
        }
        finally
        {
            QueueLock.Release();
        }
    }

    public async Task<QueueStatusResponse> GetStatusAsync(long userId)
    {
        var entry = await _queueStore.GetAsync(userId);
        if (entry == null)
        {
            return new QueueStatusResponse { InQueue = false, Difficulty = null, WaitingSeconds = 0 };
        }
        return ToStatus(entry);
    }

    private static QueueStatusResponse ToStatus(QueueEntry entry)
    {
        return new QueueStatusResponse
        {
            InQueue = true,
            Difficulty = entry.Difficulty,
            WaitingSeconds = (int)(DateTime.UtcNow - entry.JoinedAt).TotalSeconds
        };
    }
}