using Microsoft.EntityFrameworkCore;
using NullBattle.Duels.Data;
using NullBattle.Duels.Models;

namespace NullBattle.Duels.Repositories;

public class DuelRepository
{
    private readonly DuelDbContext _db;

    public DuelRepository(DuelDbContext db)
    {
        _db = db;
    }

    public async Task<Duel?> GetByIdAsync(long id)
    {
        return await _db.Duels
            .Include(d => d.Categories)
            .Include(d => d.Players)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<Duel>> GetByUserIdAsync(long userId)
    {
        return await _db.Duels
            .Include(d => d.Categories)
            .Include(d => d.Players)
            .Where(d => d.Players.Any(p => p.UserId == userId))
            .OrderByDescending(d => d.Id)
            .ToListAsync();
    }

    public async Task<bool> HasActiveDuelAsync(long userId)
    {
        return await _db.DuelUsers.AnyAsync(u =>
            u.UserId == userId &&
            (u.Duel.Status == DuelStatus.BANNING || u.Duel.Status == DuelStatus.IN_PROGRESS));
    }

    public async Task AddAsync(Duel duel)
    {
        _db.Duels.Add(duel);
        await _db.SaveChangesAsync();
    }

    public async Task SaveAsync()
    {
        await _db.SaveChangesAsync();
    }

    public async Task<int> CountSubmissionsAsync(long duelUserId)
    {
        return await _db.Submissions.CountAsync(s => s.DuelUserId == duelUserId);
    }

    public void AddSubmission(Submission submission)
    {
        _db.Submissions.Add(submission);
    }
    public async Task<List<Duel>> GetExpiredAsync()
    {
        var now = DateTime.UtcNow;
        return await _db.Duels
            .Include(d => d.Categories)
            .Include(d => d.Players)
            .Where(d => (d.Status == DuelStatus.BANNING || d.Status == DuelStatus.IN_PROGRESS)
                        && d.ExpiresAt < now)
            .ToListAsync();
    }
}