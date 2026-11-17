using Microsoft.EntityFrameworkCore;
using NullBattle.Ratings.Models;

namespace NullBattle.Ratings.Data;

public class RatingDbContext : DbContext
{
    public RatingDbContext(DbContextOptions<RatingDbContext> options) : base(options)
    {
    }

    public DbSet<PlayerRating> Ratings => Set<PlayerRating>();
    public DbSet<RatingHistory> RatingHistory => Set<RatingHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerRating>(entity =>
        {
            entity.ToTable("ratings");
            entity.HasIndex(r => r.UserId).IsUnique();
        });

        modelBuilder.Entity<RatingHistory>(entity =>
        {
            entity.ToTable("ratings_history");
            entity.HasOne(h => h.Player)
                  .WithMany(r => r.History)
                  .HasForeignKey(h => h.RatingId);

            entity.HasIndex(h => new { h.RatingId, h.DuelId }).IsUnique();
        });
    }
}