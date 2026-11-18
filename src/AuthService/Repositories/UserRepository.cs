using Microsoft.EntityFrameworkCore;
using NullBattle.Auth.Data;
using NullBattle.Auth.Models;

namespace NullBattle.Auth.Repositories;

public class UserRepository
{
    private readonly UserDbContext _db;

    public UserRepository(UserDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByIdAsync(long id)
    {
        return await _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByNicknameAsync(string nickname)
    {
        return await _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Nickname == nickname);
    }

    public async Task AddAsync(User user, string roleName)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null)
        {
            throw new InvalidOperationException($"Роль {roleName} не найдена");
        }

        user.CreatedAt = DateTime.UtcNow;
        user.UserRoles.Add(new UserRole
        {
            Role = role,
            AssignedAt = DateTime.UtcNow
        });

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).OrderBy(u => u.Id).ToListAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }
}