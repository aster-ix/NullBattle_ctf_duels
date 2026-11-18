using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NullBattle.Matchmaking.Services;

namespace NullBattle.Matchmaking.Controllers;

[ApiController]
[Route("api/matchmaking")]
[Authorize]
public class MatchmakingController : ControllerBase
{
    private readonly MatchmakingService _matchmakingService;

    public MatchmakingController(MatchmakingService matchmakingService)
    {
        _matchmakingService = matchmakingService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userId = long.Parse(User.FindFirst("sub")!.Value);
        var response = await _matchmakingService.GetStatusAsync(userId);
        return Ok(response);
    }
}