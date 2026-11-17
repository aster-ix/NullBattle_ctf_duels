
namespace NullBattle.Duels.DTOs;

public class SubmitFlagResult
{
    public bool IsCorrect { get; set; }
    public int AttemptNumber { get; set; }
    public DuelResponse Duel { get; set; } = null!;
}