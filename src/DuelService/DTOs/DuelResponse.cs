using NullBattle.Duels.Models;

namespace NullBattle.Duels.DTOs;

public class DuelResponse
{
    public long Id { get; set; }
    public DuelStatus Status { get; set; }
    public Difficulty Difficulty { get; set; }
    public List<long> PlayerIds { get; set; } = new();
    public long? TurnUserId { get; set; }
    public List<DuelCategoryDto> Categories { get; set; } = new();
    public TaskResponse? Task { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public long? WinnerUserId { get; set; }
}