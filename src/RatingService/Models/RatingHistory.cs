namespace NullBattle.Ratings.Models;

public class RatingHistory
{
    public long Id { get; set; }
    public long RatingId { get; set; }
    public long DuelId { get; set; }
    public int OldRating { get; set; }
    public int NewRating { get; set; }
    public int Delta { get; set; }

    public PlayerRating Player { get; set; } = null!;
}