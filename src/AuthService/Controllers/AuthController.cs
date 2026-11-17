using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NullBattle.Auth.DTOs;
using NullBattle.Auth.Services;

namespace NullBattle.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(AuthRequest dto)
    {
        var response = await _authService.RegisterAsync(dto);
        return StatusCode(201, response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(AuthRequest dto)
    {
        var response = await _authService.LoginAsync(dto);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = long.Parse(User.FindFirst("sub")!.Value);
        var user = await _authService.GetUserAsync(userId);
        return Ok(user);
    }
}