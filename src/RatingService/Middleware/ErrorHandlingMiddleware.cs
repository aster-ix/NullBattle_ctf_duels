using NullBattle.Ratings.Exceptions;
using NullBattle.Ratings.Messaging;

namespace NullBattle.Ratings.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    private readonly LogPublisher _logPublisher;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, LogPublisher logPublisher)
    {
        _next = next;
        _logger = logger;
        _logPublisher = logPublisher;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (BadHttpRequestException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            await context.Response.WriteAsJsonAsync(new { error = "некорректный запрос или файл слишком большой" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанная ошибка");
            _logPublisher.Error($"{context.Request.Method} {context.Request.Path}: {ex.Message}");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = "Внутренняя ошибка сервера" });
        }
    }
}