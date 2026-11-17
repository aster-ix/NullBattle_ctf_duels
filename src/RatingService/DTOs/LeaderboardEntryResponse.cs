namespace NullBattle.Ratings.DTOs;

public class LeaderboardEntryResponse
{
    public int Place { get; set; }
    public long UserId { get; set; }
    public int Rating { get; set; }
    public int TotalPlayed { get; set; }
    public int Wins { get; set; }
}