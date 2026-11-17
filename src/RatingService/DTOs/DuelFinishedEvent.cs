namespace NullBattle.Ratings.DTOs;

public class DuelFinishedEvent
{
    public long DuelId { get; set; }
    public long Player1Id { get; set; }
    public long Player2Id { get; set; }
    public long? WinnerUserId { get; set; }
    public DateTime FinishedAt { get; set; }
}