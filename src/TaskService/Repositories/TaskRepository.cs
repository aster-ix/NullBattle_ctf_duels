using Microsoft.EntityFrameworkCore;
using NullBattle.TaskBank.Data;
using NullBattle.TaskBank.Models;

namespace NullBattle.TaskBank.Repositories;

public class TaskRepository
{
    private readonly TaskDbContext _db;

    public TaskRepository(TaskDbContext db)
    {
        _db = db;
    }

    public async Task<CtfTask?> GetByIdAsync(long id)
    {
        return await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<CtfTask?> GetRandomAsync(long categoryId, Difficulty difficulty)
    {
        return await _db.Tasks
            .Where(t => t.CategoryId == categoryId && t.Difficulty == difficulty)
            .OrderBy(t => EF.Functions.Random())
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(CtfTask task)
    {
        task.CreatedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(CtfTask task)
    {
        task.UpdatedAt = DateTime.UtcNow;
        _db.Tasks.Update(task);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> CategoryExistsAsync(long categoryId)
    {
        return await _db.Categories.AnyAsync(c => c.Id == categoryId);
    }

    public async Task<List<Category>> GetRandomCategoriesAsync(int count)
    {
        return await _db.Categories
            .OrderBy(c => EF.Functions.Random())
            .Take(count)
            .ToListAsync();
    }
}