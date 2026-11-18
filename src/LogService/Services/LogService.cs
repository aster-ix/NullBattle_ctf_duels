using NullBattle.Logs.Dtos;
using NullBattle.Logs.DTOs;
using NullBattle.Logs.Exceptions;
using NullBattle.Logs.Models;
using NullBattle.Logs.Repositories;

namespace NullBattle.Logs.Services;

public class LogService
{
    private static readonly string[] AllowedLevels = { "INFO", "WARN", "ERROR" };

    private readonly LogRepository _logRepository;

    public LogService(LogRepository logRepository)
    {
        _logRepository = logRepository;
    }

    public async Task SaveAsync(LogMessage message)
    {
        var service = message.Service.Trim();
        var level = message.Level.Trim().ToUpper();
        var text = message.Message.Trim();

        if (service.Length == 0 || service.Length > 50)
        {
            throw new AppException(400, "Название сервиса должно быть от 1 до 50 символов");
        }
        if (!AllowedLevels.Contains(level))
        {
            throw new AppException(400, "Уровень должен быть INFO, WARN или ERROR");
        }
        if (text.Length == 0)
        {
            throw new AppException(400, "Текст записи пустой");
        }
        if (text.Length > 1000)
        {
            text = text.Substring(0, 1000);
        }

        var entry = new LogEntry
        {
            Service = service,
            Level = level,
            Message = text,
            CreatedAt = message.CreatedAt == default ? DateTime.UtcNow : message.CreatedAt.ToUniversalTime()
        };
        await _logRepository.AddAsync(entry);
    }

    public async Task<List<LogResponse>> GetLogsAsync(string? service, string? level, int count)
    {
        if (count < 1 || count > 500)
        {
            throw new AppException(400, "count должен быть от 1 до 500");
        }

        var logs = await _logRepository.GetAsync(service, level?.Trim().ToUpper(), count);

        return logs
            .Select(l => new LogResponse
            {
                Id = l.Id,
                Service = l.Service,
                Level = l.Level,
                Message = l.Message,
                CreatedAt = l.CreatedAt
            }).ToList();
    }
}