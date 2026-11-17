namespace NullBattle.Ratings.DTOs;

public class RatingHistoryResponse
{
    public long DuelId { get; set; }
    public int OldRating { get; set; }
    public int NewRating { get; set; }
    public int Delta { get; set; }
}