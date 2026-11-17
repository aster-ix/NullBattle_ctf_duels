namespace NullBattle.Duels.Models;

public class DuelUser
{
    public long Id { get; set; }
    public long DuelId { get; set; }
    public long UserId { get; set; }
    public DuelResult? Result { get; set; }

    public Duel Duel { get; set; } = null!;
    public List<Submission> Submissions { get; set; } = new();
}