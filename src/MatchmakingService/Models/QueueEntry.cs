namespace NullBattle.Matchmaking.Models;

public class QueueEntry
{
    public long UserId { get; set; }
    public int Rating { get; set; }
    public Difficulty Difficulty { get; set; }
    public string ConnectionId { get; set; } = "";
    public DateTime JoinedAt { get; set; }
}