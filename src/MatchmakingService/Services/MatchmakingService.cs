using NullBattle.Matchmaking.Clients;
using NullBattle.Matchmaking.Dtos;
using NullBattle.Matchmaking.DTOs;
using NullBattle.Matchmaking.Exceptions;
using NullBattle.Matchmaking.Messaging;
using NullBattle.Matchmaking.Models;
using NullBattle.Matchmaking.Storage;

namespace NullBattle.Matchmaking.Services;

public class MatchmakingService
{
    private const int BaseRatingDiff = 100;
    private const int RatingDiffPerSecond = 10;

    private static readonly SemaphoreSlim QueueLock = new SemaphoreSlim(1, 1);

    private readonly QueueStore _queueStore;
    private readonly RatingServiceClient _ratingClient;
    private readonly DuelServiceClient _duelClient;
    private readonly MatchNotifier _notifier;
    private readonly ILogger<MatchmakingService> _logger;

    private readonly LogPublisher _logPublisher;

    public MatchmakingService(
        QueueStore queueStore,
        RatingServiceClient ratingClient,
        DuelServiceClient duelClient,
        MatchNotifier notifier,
        ILogger<MatchmakingService> logger,
        LogPublisher logPublisher)
    {
        _queueStore = queueStore;
        _ratingClient = ratingClient;
        _duelClient = duelClient;
        _notifier = notifier;
        _logger = logger;
        _logPublisher = logPublisher;
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

    public async Task FindMatchesAsync()
    {
        await QueueLock.WaitAsync();
        try
        {
            var entries = await _queueStore.GetAllAsync();
            var now = DateTime.UtcNow;

            foreach (var group in entries.GroupBy(e => e.Difficulty))
            {
                var players = group.ToList();

                var i = 0;
                while (i < players.Count - 1)
                {
                    if (CanMatch(players[i], players[i + 1], now))
                    {
                        await CreateMatchAsync(players[i], players[i + 1]);
                        i += 2;
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }
        finally
        {
            QueueLock.Release();
        }
    }

    private static bool CanMatch(QueueEntry a, QueueEntry b, DateTime now)
    {
        var waited = Math.Max((now - a.JoinedAt).TotalSeconds, (now - b.JoinedAt).TotalSeconds);
        var allowedDiff = BaseRatingDiff + waited * RatingDiffPerSecond;

        return Math.Abs(a.Rating - b.Rating) <= allowedDiff;
    }

    private async Task CreateMatchAsync(QueueEntry first, QueueEntry second)
    {
        await _queueStore.RemoveAsync(first.UserId);
        await _queueStore.RemoveAsync(second.UserId);

        var player1 = first;
        var player2 = second;
        if (Random.Shared.Next(2) == 0)
        {
            player1 = second;
            player2 = first;
        }

        long duelId;
        try
        {
            duelId = await _duelClient.CreateDuelAsync(player1.UserId, player2.UserId, first.Difficulty);
        }
        catch (AppException ex)
        {
            _logger.LogWarning("Не удалось создать дуэль для {A} и {B}: {Message}",
                player1.UserId, player2.UserId, ex.Message);
            await _notifier.NotifyMatchFailedAsync(first.ConnectionId);
            await _notifier.NotifyMatchFailedAsync(second.ConnectionId);
            _logPublisher.Warn($"Не удалось создать дуэль для {player1.UserId} и {player2.UserId}: {ex.Message}");
            return;
        }

        _logger.LogInformation("Найдена пара {A} и {B}, дуэль {DuelId}", player1.UserId, player2.UserId, duelId);
        _logPublisher.Info($"Найдена пара {player1.UserId} и {player2.UserId}, дуэль {duelId}");
        await _notifier.NotifyMatchFoundAsync(first.ConnectionId, duelId);
        await _notifier.NotifyMatchFoundAsync(second.ConnectionId, duelId);
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