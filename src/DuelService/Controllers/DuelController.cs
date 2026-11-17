using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NullBattle.Duels.DTOs;
using NullBattle.Duels.Services;

namespace NullBattle.Duels.Controllers;

[ApiController]
[Route("api/duels")]
[Authorize]
public class DuelController : ControllerBase
{
    private readonly DuelService _duelService;

    public DuelController(DuelService duelService)
    {
        _duelService = duelService;
    }


    [HttpPost]
    [Authorize(Roles = "Service,Admin")]
    public async Task<IActionResult> CreateDuel(CreateDuelRequest dto)
    {
        var response = await _duelService.CreateDuelAsync(dto);
        return Created($"/api/duels/{response.Id}", response);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetDuel(long id)
    {
        var userId = long.Parse(User.FindFirst("sub")!.Value);
        var response = await _duelService.GetDuelAsync(id, userId);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetHistory(long userId)
    {
        var response = await _duelService.GetHistoryAsync(userId);
        return Ok(response);
    }
}