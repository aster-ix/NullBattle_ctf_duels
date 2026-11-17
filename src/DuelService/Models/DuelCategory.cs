namespace NullBattle.Duels.Models;

public class DuelCategory
{
    public long Id { get; set; }
    public long DuelId { get; set; }
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public long? BannedByUserId { get; set; }

    public Duel Duel { get; set; } = null!;
}