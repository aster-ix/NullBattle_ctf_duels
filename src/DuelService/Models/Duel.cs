namespace NullBattle.Duels.Models;

public class Duel
{
    public long Id { get; set; }
    public DuelStatus Status { get; set; }
    public Difficulty Difficulty { get; set; }
    public long? TaskId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public long? WinnerUserId { get; set; }
    public DateTime ExpiresAt { get; set; }

    public List<DuelCategory> Categories { get; set; } = new();
    public List<DuelUser> Players { get; set; } = new();
}