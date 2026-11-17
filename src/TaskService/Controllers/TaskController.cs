using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NullBattle.TaskBank.DTOs;
using NullBattle.TaskBank.Models;
using NullBattle.TaskBank.Services;

namespace NullBattle.TaskBank.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly TaskService _taskService;

    public TaskController(TaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpPost("tasks")]
    [Authorize(Roles = "Author")]
    public async Task<IActionResult> CreateTask(TaskRequest dto)
    {
        var authorId = long.Parse(User.FindFirst("sub")!.Value);
        var response = await _taskService.CreateTaskAsync(authorId, dto);
        return StatusCode(201, response);
    }

    [HttpPut("tasks/{id:long}")]
    [Authorize(Roles = "Author")]
    public async Task<IActionResult> UpdateTask(long id, TaskRequest dto)
    {
        var response = await _taskService.UpdateTaskAsync(id, dto);
        return Ok(response);
    }

    [HttpGet("tasks/{id:long}")]
    [Authorize(Roles = "Author,Service")]
    public async Task<IActionResult> GetTask(long id)
    {
        var response = await _taskService.GetTaskAsync(id);
        return Ok(response);
    }

    [HttpGet("categories/random")]
    [Authorize(Roles = "Service")]
    public async Task<IActionResult> GetRandomCategories(int count = 3)
    {
        var response = await _taskService.GetRandomCategoriesAsync(count);
        return Ok(response);
    }

    [HttpGet("tasks/random")]
    [Authorize(Roles = "Service")]
    public async Task<IActionResult> GetRandomTask(long categoryId, Difficulty difficulty)
    {
        var response = await _taskService.GetRandomTaskAsync(categoryId, difficulty);
        return Ok(response);
    }

    [HttpPost("tasks/{id:long}/verify-flag")]
    [Authorize(Roles = "Service")]
    public async Task<IActionResult> VerifyFlag(long id, VerifyFlagRequest dto)
    {
        var response = await _taskService.VerifyFlagAsync(id, dto.Flag);
        return Ok(response);
    }

    [HttpPut("tasks/{id:long}/file")]
    [Authorize(Roles = "Author")]
    public async Task<IActionResult> UploadFile(long id, IFormFile? file)
    {
        await _taskService.UploadFileAsync(id, file);
        return NoContent();
    }

    [HttpGet("tasks/{id:long}/file")]
    public async Task<IActionResult> DownloadFile(long id)
    {
        var file = await _taskService.GetFileAsync(id);
        return PhysicalFile(file.Path, "application/octet-stream", file.FileName);
    }
}