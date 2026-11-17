using NullBattle.Matchmaking.Models;

namespace NullBattle.Matchmaking.DTOs;

public class QueueStatusResponse
{
    public bool InQueue { get; set; }
    public Difficulty? Difficulty { get; set; }
    public int WaitingSeconds { get; set; }
}