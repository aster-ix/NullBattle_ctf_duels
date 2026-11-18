using NullBattle.Duels.Clients;
using NullBattle.Duels.DTOs;
using NullBattle.Duels.Exceptions;
using NullBattle.Duels.Models;
using NullBattle.Duels.Repositories;
using NullBattle.Duels.Messaging;

namespace NullBattle.Duels.Services;

public class DuelService
{
    private const int MaxAttempts = 20;
    private static readonly SemaphoreSlim FinishLock = new SemaphoreSlim(1, 1);
    private const int BanPhaseMinutes = 2;

    private readonly DuelRepository _duelRepository;
    private readonly TaskServiceClient _taskClient;
    private readonly DuelEventPublisher _eventPublisher;

    private readonly LogPublisher _logPublisher;

    public DuelService(
        DuelRepository duelRepository,
        TaskServiceClient taskClient,
        DuelEventPublisher eventPublisher,
        LogPublisher logPublisher)
    {
        _duelRepository = duelRepository;
        _taskClient = taskClient;
        _eventPublisher = eventPublisher;
        _logPublisher = logPublisher;
    }

    public async Task<DuelResponse> CreateDuelAsync(CreateDuelRequest dto)
    {
        if (dto.Player1Id == dto.Player2Id)
        {
            throw new AppException(400, "Игроки должны быть разными");
        }

        if (await _duelRepository.HasActiveDuelAsync(dto.Player1Id)
            || await _duelRepository.HasActiveDuelAsync(dto.Player2Id))
        {
            throw new AppException(409, "Один из игроков уже участвует в незавершённой дуэли");
        }

        var categories = await _taskClient.GetRandomCategoriesAsync(3);
        if (categories.Count < 3)
        {
            throw new AppException(502, "TaskService не вернул категории");
        }

        var duel = new Duel
        {
            Status = DuelStatus.BANNING,
            Difficulty = dto.Difficulty,
            ExpiresAt = DateTime.UtcNow.AddMinutes(BanPhaseMinutes)
        };
        foreach (var category in categories)
        {
            duel.Categories.Add(new DuelCategory
            {
                CategoryId = category.Id,
                CategoryName = category.Name
            });
        }
        duel.Players.Add(new DuelUser { UserId = dto.Player1Id });
        duel.Players.Add(new DuelUser { UserId = dto.Player2Id });

        await _duelRepository.AddAsync(duel);
        _logPublisher.Info($"Создана дуэль {duel.Id}: игроки {dto.Player1Id} и {dto.Player2Id}, сложность {dto.Difficulty}");
        return ToResponse(duel);
    }

    public async Task<DuelResponse> GetDuelAsync(long duelId, long userId)
    {
        var duel = await _duelRepository.GetByIdAsync(duelId);
        if (duel == null)
        {
            throw new AppException(404, "Дуэль не найдена");
        }
        if (!duel.Players.Any(p => p.UserId == userId))
        {
            throw new AppException(403, "Вы не участник этой дуэли");
        }

        var response = ToResponse(duel);
        if (duel.TaskId != null)
        {
            response.Task = await _taskClient.GetTaskAsync(duel.TaskId.Value);
        }
        return response;
    }

    public async Task<List<DuelResponse>> GetHistoryAsync(long userId)
    {
        if (userId <= 0)
        {
            throw new AppException(400, "userId должен быть положительным числом");
        }

        var duels = await _duelRepository.GetByUserIdAsync(userId);
        return duels.Select(ToResponse).ToList();
    }
    public async Task<SubmitFlagResult> SubmitFlagAsync(long duelId, long userId, string flag)
    {
        flag = (flag ?? "").Trim();
        if (flag.Length == 0 || flag.Length > 255)
        {
            throw new AppException(400, "Флаг должен быть от 1 до 255 символов");
        }

        await FinishLock.WaitAsync();
        try
        {
            var duel = await _duelRepository.GetByIdAsync(duelId);
            if (duel == null)
            {
                throw new AppException(404, "Дуэль не найдена");
            }

            var player = duel.Players.FirstOrDefault(p => p.UserId == userId);
            if (player == null)
            {
                throw new AppException(403, "Вы не участник этой дуэли");
            }
            if (duel.Status != DuelStatus.IN_PROGRESS)
            {
                throw new AppException(409, "Сейчас нельзя отправлять флаги");
            }
            if (duel.ExpiresAt < DateTime.UtcNow)
            {
                throw new AppException(409, "Время дуэли вышло");
            }

            var attempts = await _duelRepository.CountSubmissionsAsync(player.Id);
            if (attempts >= MaxAttempts)
            {
                throw new AppException(409, "Лимит попыток исчерпан");
            }

            var isCorrect = await _taskClient.VerifyFlagAsync(duel.TaskId!.Value, flag);

            _duelRepository.AddSubmission(new Submission
            {
                DuelUserId = player.Id,
                AttemptNumber = attempts + 1,
                IsCorrect = isCorrect,
                SubmittedAt = DateTime.UtcNow
            });

            if (isCorrect)
            {
                duel.Status = DuelStatus.FINISHED;
                duel.FinishedAt = DateTime.UtcNow;
                duel.WinnerUserId = userId;
                foreach (var p in duel.Players)
                {
                    p.Result = p.UserId == userId ? DuelResult.WIN : DuelResult.LOSE;
                }
            }

            await _duelRepository.SaveAsync();

            if (isCorrect)
            {
                _logPublisher.Info($"Дуэль {duel.Id} завершена, победитель {userId}");
                await PublishFinishedAsync(duel);
            }

            return new SubmitFlagResult
            {
                IsCorrect = isCorrect,
                AttemptNumber = attempts + 1,
                Duel = ToResponse(duel)
            };
        }
        finally
        {
            FinishLock.Release();
        }
    }

    public async Task<List<DuelResponse>> FinishExpiredDuelsAsync()
    {
        await FinishLock.WaitAsync();
        try
        {
            var duels = await _duelRepository.GetExpiredAsync();

            foreach (var duel in duels)
            {
                if (duel.Status == DuelStatus.BANNING)
                {
                    duel.Status = DuelStatus.CANCELLED;
                }
                else
                {
                    duel.Status = DuelStatus.FINISHED;
                    foreach (var player in duel.Players)
                    {
                        player.Result = DuelResult.DRAW;
                    }
                }
                duel.FinishedAt = DateTime.UtcNow;
            }

            await _duelRepository.SaveAsync();

            foreach (var duel in duels)
            {
                if (duel.Status == DuelStatus.CANCELLED)
                {
                    _logPublisher.Warn($"Дуэль {duel.Id} отменена: время на баны вышло");
                }
                else
                {
                    _logPublisher.Info($"Дуэль {duel.Id} завершена ничьей: время вышло");
                }
            }

            foreach (var duel in duels.Where(d => d.Status == DuelStatus.FINISHED))
            {
                await PublishFinishedAsync(duel);
            }

            return duels.Select(ToResponse).ToList();
        }
        finally
        {
            FinishLock.Release();
        }
    }

    private async Task PublishFinishedAsync(Duel duel)
    {
        var players = duel.Players.OrderBy(p => p.Id).ToList();

        await _eventPublisher.PublishDuelFinishedAsync(new DuelFinishedEvent
        {
            DuelId = duel.Id,
            Player1Id = players[0].UserId,
            Player2Id = players[1].UserId,
            WinnerUserId = duel.WinnerUserId,
            FinishedAt = duel.FinishedAt!.Value
        });
    }

    private static long? GetTurnUserId(Duel duel)
    {
        if (duel.Status != DuelStatus.BANNING)
        {
            return null;
        }

        var players = duel.Players.OrderBy(p => p.Id).ToList();
        var bannedCount = duel.Categories.Count(c => c.BannedByUserId != null);

        if (bannedCount == 0)
        {
            return players[0].UserId;
        }
        if (bannedCount == 1)
        {
            return players[1].UserId;
        }
        return null;
    }

    private static DuelResponse ToResponse(Duel duel)
    {
        return new DuelResponse
        {
            Id = duel.Id,
            Status = duel.Status,
            Difficulty = duel.Difficulty,
            PlayerIds = duel.Players.OrderBy(p => p.Id).Select(p => p.UserId).ToList(),
            TurnUserId = GetTurnUserId(duel),
            Categories = duel.Categories.OrderBy(c => c.Id).Select(c => new DuelCategoryDto
                {
                    Id = c.CategoryId,
                    Name = c.CategoryName,
                    BannedByUserId = c.BannedByUserId
                }).ToList(),
            ExpiresAt = duel.ExpiresAt,
            StartedAt = duel.StartedAt,
            FinishedAt = duel.FinishedAt,
            WinnerUserId = duel.WinnerUserId
        };
    }

    public async Task<BanResult> BanCategoryAsync(long duelId, long userId, long categoryId)
    {
        var duel = await _duelRepository.GetByIdAsync(duelId);
        if (duel == null)
        {
            throw new AppException(404, "Дуэль не найдена");
        }
        if (!duel.Players.Any(p => p.UserId == userId))
        {
            throw new AppException(403, "Вы не участник этой дуэли");
        }
        if (duel.Status != DuelStatus.BANNING)
        {
            throw new AppException(409, "Сейчас нельзя банить категории");
        }
        if (duel.ExpiresAt < DateTime.UtcNow)
        {
            throw new AppException(409, "Время на баны вышло");
        }
        if (GetTurnUserId(duel) != userId)
        {
            throw new AppException(409, "Сейчас не ваш ход");
        }

        var category = duel.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
        if (category == null)
        {
            throw new AppException(400, "В этой дуэли нет такой категории");
        }
        if (category.BannedByUserId != null)
        {
            throw new AppException(409, "Эта категория уже забанена");
        }

        category.BannedByUserId = userId;

        TaskResponse? task = null;
        var bannedCount = duel.Categories.Count(c => c.BannedByUserId != null);
        if (bannedCount == 2)
        {
            var left = duel.Categories.First(c => c.BannedByUserId == null);
            task = await _taskClient.GetRandomTaskAsync(left.CategoryId, duel.Difficulty);

            duel.TaskId = task.Id;
            duel.Status = DuelStatus.IN_PROGRESS;
            duel.StartedAt = DateTime.UtcNow;
            duel.ExpiresAt = duel.StartedAt.Value.AddMinutes(GetDurationMinutes(duel.Difficulty));
        }

        await _duelRepository.SaveAsync();
        if (task != null)
        {
            _logPublisher.Info($"Дуэль {duel.Id} началась, задача {task.Id}");
        }
        var response = ToResponse(duel);
        response.Task = task;

        return new BanResult { Duel = response, Started = task != null };
    }

    private static int GetDurationMinutes(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.EASY:
                return 30;
            case Difficulty.MEDIUM:
                return 45;
            default:
                return 60;
        }
    }
}