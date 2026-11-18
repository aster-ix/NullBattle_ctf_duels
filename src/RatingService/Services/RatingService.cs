using NullBattle.Ratings.DTOs;
using NullBattle.Ratings.Exceptions;
using NullBattle.Ratings.Messaging;
using NullBattle.Ratings.Models;
using NullBattle.Ratings.Repositories;

namespace NullBattle.Ratings.Services;

public class RatingService
{
    private const int StartRating = 1000;

    private readonly RatingRepository _ratingRepository;
    private readonly EloCalculator _eloCalculator;

    private readonly LogPublisher _logPublisher;

    public RatingService(
        RatingRepository ratingRepository,
        EloCalculator eloCalculator,
        LogPublisher logPublisher)
    {
        _ratingRepository = ratingRepository;
        _eloCalculator = eloCalculator;
        _logPublisher = logPublisher;
    }

    public async Task<RatingResponse> GetRatingAsync(long userId)
    {
        CheckUserId(userId);

        var rating = await _ratingRepository.GetByUserIdAsync(userId);
        if (rating == null)
        {

            return new RatingResponse { UserId = userId, Rating = StartRating };
        }

        return new RatingResponse
        {
            UserId = rating.UserId,
            Rating = rating.Rating,
            TotalPlayed = rating.TotalPlayed,
            Wins = rating.Wins,
            Losses = rating.Losses,
            Draws = rating.Draws
        };
    }

    public async Task<List<RatingHistoryResponse>> GetHistoryAsync(long userId)
    {
        CheckUserId(userId);

        var history = await _ratingRepository.GetHistoryByUserIdAsync(userId);
        return history
            .Select(h => new RatingHistoryResponse
            {
                DuelId = h.DuelId,
                OldRating = h.OldRating,
                NewRating = h.NewRating,
                Delta = h.Delta
            })
            .ToList();
    }

    public async Task<List<LeaderboardEntryResponse>> GetTopAsync(int count)
    {
        if (count < 1 || count > 100)
        {
            throw new AppException(400, "count должен быть от 1 до 100");
        }

        var top = await _ratingRepository.GetTopAsync(count);

        var result = new List<LeaderboardEntryResponse>();
        for (int i = 0; i < top.Count; i++)
        {
            result.Add(new LeaderboardEntryResponse
            {
                Place = i + 1,
                UserId = top[i].UserId,
                Rating = top[i].Rating,
                TotalPlayed = top[i].TotalPlayed,
                Wins = top[i].Wins
            });
        }
        return result;
    }

    public async Task ApplyDuelResultAsync(DuelFinishedEvent duel)
    {
        if (duel.Player1Id <= 0 || duel.Player2Id <= 0 || duel.Player1Id == duel.Player2Id)
        {
            throw new AppException(400, "Некорректные игроки дуэли");
        }
        if (duel.WinnerUserId != null
            && duel.WinnerUserId != duel.Player1Id
            && duel.WinnerUserId != duel.Player2Id)
        {
            throw new AppException(400, "Победитель не участвовал в дуэли");
        }

        if (await _ratingRepository.HistoryExistsAsync(duel.Player1Id, duel.DuelId))
        {
            return;
        }

        var player1 = await GetOrCreateAsync(duel.Player1Id);
        var player2 = await GetOrCreateAsync(duel.Player2Id);

        DuelResult result1;
        DuelResult result2;
        if (duel.WinnerUserId == null)
        {
            result1 = DuelResult.DRAW;
            result2 = DuelResult.DRAW;
        }
        else if (duel.WinnerUserId == duel.Player1Id)
        {
            result1 = DuelResult.WIN;
            result2 = DuelResult.LOSE;
        }
        else
        {
            result1 = DuelResult.LOSE;
            result2 = DuelResult.WIN;
        }

        // тут оба изменения считаю по старым рейтингам, до того как что-то поменяли
        int delta1 = _eloCalculator.Calculate(player1.Rating, player2.Rating, result1);
        int delta2 = _eloCalculator.Calculate(player2.Rating, player1.Rating, result2);

        Apply(player1, duel.DuelId, delta1, result1);
        Apply(player2, duel.DuelId, delta2, result2);

     
        await _ratingRepository.SaveAsync();
        _logPublisher.Info($"Рейтинг обновлён по дуэли {duel.DuelId}: игрок {duel.Player1Id} ({delta1}), игрок {duel.Player2Id} ({delta2})");
    }

    private async Task<PlayerRating> GetOrCreateAsync(long userId)
    {
        var rating = await _ratingRepository.GetByUserIdAsync(userId);
        if (rating == null)
        {
            rating = new PlayerRating { UserId = userId, Rating = StartRating };
            _ratingRepository.Add(rating);
        }
        return rating;
    }

    private void Apply(PlayerRating player, long duelId, int delta, DuelResult result)
    {
        var history = new RatingHistory
        {
            Player = player,
            DuelId = duelId,
            OldRating = player.Rating,
            NewRating = player.Rating + delta,
            Delta = delta
        };
        _ratingRepository.AddHistory(history);

        player.Rating += delta;
        player.TotalPlayed++;
        player.UpdatedAt = DateTime.UtcNow;

        if (result == DuelResult.WIN)
        {
            player.Wins++;
        }
        else if (result == DuelResult.LOSE)
        {
            player.Losses++;
        }
        else
        {
            player.Draws++;
        }
    }

    private static void CheckUserId(long userId)
    {
        if (userId <= 0)
        {
            throw new AppException(400, "userId должен быть положительным числом");
        }
    }
}