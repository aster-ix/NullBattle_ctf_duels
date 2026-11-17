using Microsoft.EntityFrameworkCore;
using NullBattle.Ratings.Data;
using NullBattle.Ratings.Models;

namespace NullBattle.Ratings.Repositories;

public class RatingRepository
{
    private readonly RatingDbContext _db;

    public RatingRepository(RatingDbContext db)
    {
        _db = db;
    }

    public async Task<PlayerRating?> GetByUserIdAsync(long userId)
    {
        return await _db.Ratings.FirstOrDefaultAsync(r => r.UserId == userId);
    }

    public async Task<List<RatingHistory>> GetHistoryByUserIdAsync(long userId)
    {
        return await _db.RatingHistory
            .Where(h => h.Player.UserId == userId)
            .OrderByDescending(h => h.Id)
            .ToListAsync();
    }

    public async Task<List<PlayerRating>> GetTopAsync(int count)
    {
        return await _db.Ratings
            .OrderByDescending(r => r.Rating)
            .ThenByDescending(r => r.Wins)
            .ThenBy(r => r.UserId)
            .Take(count)
            .ToListAsync();
    }

    public async Task<bool> HistoryExistsAsync(long userId, long duelId)
    {
        return await _db.RatingHistory
            .AnyAsync(h => h.DuelId == duelId && h.Player.UserId == userId);
    }

    public void Add(PlayerRating rating)
    {
        _db.Ratings.Add(rating);
    }

    public void AddHistory(RatingHistory history)
    {
        _db.RatingHistory.Add(history);
    }

    public async Task SaveAsync()
    {
        await _db.SaveChangesAsync();
    }
}