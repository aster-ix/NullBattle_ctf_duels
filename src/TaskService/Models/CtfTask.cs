namespace NullBattle.TaskBank.Models;

public class CtfTask
{
    public long Id { get; set; }
    public long CategoryId { get; set; }
    public long AuthorId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public Difficulty Difficulty { get; set; }
    public int Points { get; set; }
    public string FlagHash { get; set; } = "";
    public string? FileName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Category Category { get; set; } = null!;
}