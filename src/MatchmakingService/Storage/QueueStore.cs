using System.Text.Json;
using NullBattle.Matchmaking.Models;
using StackExchange.Redis;

namespace NullBattle.Matchmaking.Storage;

public class QueueStore
{
    private const string QueueKey = "matchmaking:queue";

    private readonly IDatabase _redis;

    public QueueStore(IConnectionMultiplexer connection)
    {
        _redis = connection.GetDatabase();
    }

    public async Task AddAsync(QueueEntry entry)
    {
        var json = JsonSerializer.Serialize(entry);
        await _redis.StringSetAsync(GetPlayerKey(entry.UserId), json);
        await _redis.SortedSetAddAsync(QueueKey, entry.UserId, entry.Rating);
    }

    public async Task<QueueEntry?> GetAsync(long userId)
    {
        var json = await _redis.StringGetAsync(GetPlayerKey(userId));
        if (json.IsNullOrEmpty)
        {
            return null;
        }
        return JsonSerializer.Deserialize<QueueEntry>(json.ToString());
    }

    public async Task RemoveAsync(long userId)
    {
        await _redis.KeyDeleteAsync(GetPlayerKey(userId));
        await _redis.SortedSetRemoveAsync(QueueKey, userId);
    }

    public async Task<List<QueueEntry>> GetAllAsync()
    {
        var members = await _redis.SortedSetRangeByRankAsync(QueueKey);

        var result = new List<QueueEntry>();
        foreach (var member in members)
        {
            var entry = await GetAsync((long)member);
            if (entry != null)
            {
                result.Add(entry);
            }
        }
        return result;
    }

    public async Task ClearAsync()
    {
        var entries = await GetAllAsync();
        foreach (var entry in entries)
        {
            await RemoveAsync(entry.UserId);
        }
    }

    private static string GetPlayerKey(long userId)
    {
        return "matchmaking:player:" + userId;
    }
}