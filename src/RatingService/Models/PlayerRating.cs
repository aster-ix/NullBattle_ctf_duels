namespace NullBattle.Ratings.Models;

public class PlayerRating
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public int Rating { get; set; }
    public int TotalPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<RatingHistory> History { get; set; } = new();
}