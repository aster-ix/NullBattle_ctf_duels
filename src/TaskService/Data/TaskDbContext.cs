using Microsoft.EntityFrameworkCore;
using NullBattle.TaskBank.Models;

namespace NullBattle.TaskBank.Data;

public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CtfTask> Tasks => Set<CtfTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.Property(c => c.Name).HasMaxLength(32);
            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<CtfTask>(entity =>
        {
            entity.ToTable("tasks");
            entity.Property(t => t.Title).HasMaxLength(100);
            entity.Property(t => t.FlagHash).HasMaxLength(255);
            entity.Property(t => t.FileName).HasMaxLength(255);
            entity.Property(t => t.Difficulty).HasConversion<string>().HasMaxLength(16);
            entity.HasOne(t => t.Category)
                  .WithMany()
                  .HasForeignKey(t => t.CategoryId);
        });

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "forensics" },
            new Category { Id = 2, Name = "crypto" },
            new Category { Id = 3, Name = "osint" },
            new Category { Id = 4, Name = "stego" },
            new Category { Id = 5, Name = "reverse" },
            new Category { Id = 6, Name = "misc" });
    }
}