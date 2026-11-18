using Microsoft.EntityFrameworkCore;
using NullBattle.Logs.Models;

namespace NullBattle.Logs.Data;

public class LogDbContext : DbContext
{
    public LogDbContext(DbContextOptions<LogDbContext> options) : base(options)
    {
    }

    public DbSet<LogEntry> Logs => Set<LogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LogEntry>(entity =>
        {
            entity.ToTable("logs");
            entity.Property(l => l.Service).HasMaxLength(50);
            entity.Property(l => l.Level).HasMaxLength(10);
            entity.Property(l => l.Message).HasMaxLength(1000);
            entity.HasIndex(l => l.Service);
            entity.HasIndex(l => l.Level);
        });
    }
}