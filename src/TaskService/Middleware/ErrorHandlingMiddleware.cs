using NullBattle.TaskBank.Exceptions;

namespace NullBattle.TaskBank.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
            _logger.LogError(ex, "необработанная ошибка");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = "Внутренняя ошибка сервера" });
        }
    }
}