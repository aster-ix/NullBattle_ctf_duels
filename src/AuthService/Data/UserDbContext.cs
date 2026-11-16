using Microsoft.EntityFrameworkCore;
using NullBattle.Auth.Models;

namespace NullBattle.Auth.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.Property(u => u.Nickname).HasMaxLength(50);
            entity.Property(u => u.PasswordHash).HasMaxLength(255);
            entity.HasIndex(u => u.Nickname).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.Property(r => r.Name).HasMaxLength(64);
            entity.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("users_roles");
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });
            entity.HasOne(ur => ur.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(ur => ur.UserId);
            entity.HasOne(ur => ur.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(ur => ur.RoleId);
        });


        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Player" },
            new Role { Id = 2, Name = "Author" },
            new Role { Id = 3, Name = "Admin" });
    }
}