using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NullBattle.Ratings.DTOs;
using NullBattle.Ratings.Services;

namespace NullBattle.Ratings.Controllers;

[ApiController]
[Route("api/ratings")]
[Authorize]
public class RatingController : ControllerBase
{
    private readonly RatingService _ratingService;

    public RatingController(RatingService ratingService)
    {
        _ratingService = ratingService;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetRating(long userId)
    {
        var response = await _ratingService.GetRatingAsync(userId);
        return Ok(response);
    }

    [HttpGet("{userId}/history")]
    public async Task<IActionResult> GetHistory(long userId)
    {
        var response = await _ratingService.GetHistoryAsync(userId);
        return Ok(response);
    }

    [HttpGet("top")]
    public async Task<IActionResult> GetTop(int count = 10)
    {
        var response = await _ratingService.GetTopAsync(count);
        return Ok(response);
    }

    //// ВРЕМЕННЫЙ метод 
    //[HttpPost("apply")]
    //public async Task<IActionResult> Apply(DuelFinishedEvent dto)
    //{
    //    await _ratingService.ApplyDuelResultAsync(dto);
    //    return NoContent();
    //}
}