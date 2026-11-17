namespace NullBattle.Ratings.DTOs;

public class RatingResponse
{
    public long UserId { get; set; }
    public int Rating { get; set; }
    public int TotalPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
}