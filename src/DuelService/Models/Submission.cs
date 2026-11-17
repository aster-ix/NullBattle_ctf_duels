namespace NullBattle.Duels.Models;

public class Submission
{
    public long Id { get; set; }
    public long DuelUserId { get; set; }
    public int AttemptNumber { get; set; }
    public bool IsCorrect { get; set; }
    public DateTime SubmittedAt { get; set; }

    public DuelUser DuelUser { get; set; } = null!;
}