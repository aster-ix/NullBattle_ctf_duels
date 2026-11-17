using NullBattle.Ratings.Models;

namespace NullBattle.Ratings.Services;

public class EloCalculator
{
    private const int KFactor = 32;


    public int Calculate(int rating, int opponentRating, DuelResult result)
    {
        // от 0 до 1
        double expected = 1.0 / (1.0 + Math.Pow(10, (opponentRating - rating) / 400.0));

        double score;
        if (result == DuelResult.WIN)
        {
            score = 1.0;
        }
        else if (result == DuelResult.DRAW)
        {
            score = 0.5;
        }
        else
        {
            score = 0.0;
        }

        return (int)Math.Round(KFactor * (score - expected));
    }
}