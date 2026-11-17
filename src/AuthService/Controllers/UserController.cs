using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NullBattle.Auth.Services;

namespace NullBattle.Auth.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly AuthService _authService;

    public UsersController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _authService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpPost("{id}/ban")]
    public async Task<IActionResult> Ban(long id)
    {
        await _authService.SetBanAsync(id, true);
        return NoContent();
    }

    [HttpPost("{id}/unban")]
    public async Task<IActionResult> Unban(long id)
    {
        await _authService.SetBanAsync(id, false);
        return NoContent();
    }
}