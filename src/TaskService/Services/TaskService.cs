using NullBattle.TaskBank.Dtos;
using NullBattle.TaskBank.DTOs;
using NullBattle.TaskBank.Exceptions;
using NullBattle.TaskBank.Messaging;
using NullBattle.TaskBank.Models;
using NullBattle.TaskBank.Repositories;
using NullBattle.TaskBank.Storage;

namespace NullBattle.TaskBank.Services;

public class TaskService
{
    private const long MaxFileSize = 20 * 1024 * 1024; // загрузка до 20 мб

    private readonly TaskRepository _taskRepository;
    private readonly FlagHasher _flagHasher;
    private readonly FileStorage _fileStorage;

    private readonly LogPublisher _logPublisher;

    public TaskService(
        TaskRepository taskRepository,
        FlagHasher flagHasher,
        FileStorage fileStorage,
        LogPublisher logPublisher)
    {
        _taskRepository = taskRepository;
        _flagHasher = flagHasher;
        _fileStorage = fileStorage;
        _logPublisher = logPublisher;
    }

    public async Task<TaskResponse> CreateTaskAsync(long authorId, TaskRequest dto)
    {
        if (!await _taskRepository.CategoryExistsAsync(dto.CategoryId))
        {
            throw new AppException(404, "категория не найдена");
        }

        var task = new CtfTask
        {
            AuthorId = authorId,
            CategoryId = dto.CategoryId,
            Title = dto.Title.Trim(),
            Description = dto.Description,
            Difficulty = dto.Difficulty,
            Points = dto.Points,
            FlagHash = _flagHasher.Hash(dto.Flag.Trim())
        };
        await _taskRepository.AddAsync(task);
        _logPublisher.Info($"Автор {authorId} создал задачу {task.Id} «{task.Title}»");

        return ToResponse(task);
    }

    public async Task<TaskResponse> UpdateTaskAsync(long id, TaskRequest dto)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new AppException(404, "Задача не найдена");
        }
        if (!await _taskRepository.CategoryExistsAsync(dto.CategoryId))
        {
            throw new AppException(404, "Категория не найдена");
        }

        task.CategoryId = dto.CategoryId;
        task.Title = dto.Title.Trim();
        task.Description = dto.Description;
        task.Difficulty = dto.Difficulty;
        task.Points = dto.Points;
        task.FlagHash = _flagHasher.Hash(dto.Flag.Trim());
        await _taskRepository.UpdateAsync(task);
        _logPublisher.Info($"Изменена задача {task.Id} «{task.Title}»");
        return ToResponse(task);
    }

    public async Task<TaskResponse> GetTaskAsync(long id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new AppException(404, "задача не найдена");
        }

        return ToResponse(task);
    }

    public async Task<List<CategoryDto>> GetRandomCategoriesAsync(int count)
    {
        if (count < 1 || count > 10)
        {
            throw new AppException(400, "count должен быть от 1 до 10");
        }

        var categories = await _taskRepository.GetRandomCategoriesAsync(count);
        return categories
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
            .ToList();
    }

    public async Task<TaskResponse> GetRandomTaskAsync(long categoryId, Difficulty difficulty)
    {
        if (categoryId <= 0)
        {
            throw new AppException(400, "categoryId должен быть положительным");
        }
        if (!await _taskRepository.CategoryExistsAsync(categoryId))
        {
            throw new AppException(404, "категория не найдена");
        }

        var task = await _taskRepository.GetRandomAsync(categoryId, difficulty);
        if (task == null)
        {
            throw new AppException(404, "В категории нет задач такой сложности");
        }

        return ToResponse(task);
    }

    public async Task<VerifyFlagResponse> VerifyFlagAsync(long id, string flag)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new AppException(404, "Задача не найдена");
        }

        return new VerifyFlagResponse
        {
            IsCorrect = _flagHasher.Verify(flag.Trim(), task.FlagHash)
        };
    }

    public async Task UploadFileAsync(long id, IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            throw new AppException(400, "Файл не передан или пустой");
        }
        if (file.Length > MaxFileSize)
        {
            throw new AppException(413, "Файл слишком большой (максимум 20 МБ)");
        }

        var fileName = Path.GetFileName(file.FileName);
        if (fileName.Length > 255)
        {
            throw new AppException(400, "Слишком длинное имя файла");
        }

        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new AppException(404, "Задача не найдена");
        }

        await _fileStorage.SaveAsync(id, file);
        task.FileName = fileName;
        await _taskRepository.UpdateAsync(task);
    }

    public async Task<TaskFileDto> GetFileAsync(long id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null || task.FileName == null || !_fileStorage.Exists(id))
        {
            throw new AppException(404, "Задача не найдена или к ней не прикреплён файл");
        }

        return new TaskFileDto
        {
            Path = _fileStorage.GetPath(id),
            FileName = task.FileName
        };
    }

    private static TaskResponse ToResponse(CtfTask task)
    {
        return new TaskResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            CategoryId = task.CategoryId,
            Points = task.Points,
            Difficulty = task.Difficulty,
            HasFile = task.FileName != null,
            CreatedAt = task.CreatedAt
        };
    }
}