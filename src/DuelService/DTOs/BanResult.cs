
namespace NullBattle.Duels.DTOs;

public class BanResult
{
    public DuelResponse Duel { get; set; } = null!;
    public bool Started { get; set; }
}