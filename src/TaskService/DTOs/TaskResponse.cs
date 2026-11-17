using NullBattle.TaskBank.Models;

namespace NullBattle.TaskBank.DTOs;

public class TaskResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public long CategoryId { get; set; }
    public int Points { get; set; }
    public Difficulty Difficulty { get; set; }
    public bool HasFile { get; set; }
    public DateTime CreatedAt { get; set; }
}