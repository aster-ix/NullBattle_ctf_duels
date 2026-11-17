using Microsoft.EntityFrameworkCore;
using NullBattle.Duels.Models;

namespace NullBattle.Duels.Data;

public class DuelDbContext : DbContext
{
    public DuelDbContext(DbContextOptions<DuelDbContext> options) : base(options)
    {
    }

    public DbSet<Duel> Duels => Set<Duel>();
    public DbSet<DuelCategory> DuelCategories => Set<DuelCategory>();
    public DbSet<DuelUser> DuelUsers => Set<DuelUser>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Duel>(entity =>
        {
            entity.ToTable("duels");
            entity.Property(d => d.Status).HasConversion<string>().HasMaxLength(16);
            entity.Property(d => d.Difficulty).HasConversion<string>().HasMaxLength(16);
        });

        modelBuilder.Entity<DuelCategory>(entity =>
        {
            entity.ToTable("duels_categories");
            entity.Property(c => c.CategoryName).HasMaxLength(32);
            entity.HasOne(c => c.Duel)
                  .WithMany(d => d.Categories)
                  .HasForeignKey(c => c.DuelId);
            entity.HasIndex(c => new { c.DuelId, c.CategoryId }).IsUnique();
        });

        modelBuilder.Entity<DuelUser>(entity =>
        {
            entity.ToTable("duels_users");
            entity.Property(u => u.Result).HasConversion<string>().HasMaxLength(4);
            entity.HasOne(u => u.Duel)
                  .WithMany(d => d.Players)
                  .HasForeignKey(u => u.DuelId);
            entity.HasIndex(u => new { u.DuelId, u.UserId }).IsUnique();
            entity.HasIndex(u => u.UserId);
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.ToTable("submissions");
            entity.HasOne(s => s.DuelUser)
                  .WithMany(u => u.Submissions)
                  .HasForeignKey(s => s.DuelUserId);
        });
    }
}