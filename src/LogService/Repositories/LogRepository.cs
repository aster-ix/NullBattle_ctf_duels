using Microsoft.EntityFrameworkCore;
using NullBattle.Logs.Data;
using NullBattle.Logs.Models;

namespace NullBattle.Logs.Repositories;

public class LogRepository
{
    private readonly LogDbContext _db;

    public LogRepository(LogDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(LogEntry entry)
    {
        _db.Logs.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task<List<LogEntry>> GetAsync(string? service, string? level, int count)
    {
        var query = _db.Logs.AsQueryable();

        if (!string.IsNullOrEmpty(service))
        {
            query = query.Where(l => l.Service == service);
        }
        if (!string.IsNullOrEmpty(level))
        {
            query = query.Where(l => l.Level == level);
        }

        return await query.OrderByDescending(l => l.Id).Take(count).ToListAsync();
    }
}