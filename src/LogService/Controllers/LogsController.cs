using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NullBattle.Logs.Services;

namespace NullBattle.Logs.Controllers;

[ApiController]
[Route("api/logs")]
[Authorize(Roles = "Admin")]
public class LogsController : ControllerBase
{
    private readonly LogService _logService;

    public LogsController(LogService logService)
    {
        _logService = logService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(string? service, string? level, int count = 100)
    {
        var response = await _logService.GetLogsAsync(service, level, count);
        return Ok(response);
    }
}