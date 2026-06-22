using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WardrobeManager.API.Extensions;
using WardrobeManager.Application.Clothing.Queries;
using WardrobeManager.Application.Outfits.Commands;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/wear-events")]
[Authorize]
public sealed class WearEventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WearEventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("stats/{userId}")]
    public async Task<ActionResult<WearStatisticsDto>> GetStats(
        Guid userId,
        [FromQuery] string? range = null,
        [FromQuery] DateTime? customStart = null,
        [FromQuery] DateTime? customEnd = null,
        CancellationToken ct = default)
    {
        // catches DateTime binding failures (SuppressModelStateInvalidFilter is on).
        if (!ModelState.IsValid)
        {
            var details = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToList();

            return BadRequest(new
            {
                Error = "Invalid query parameters.",
                Details = details
            });
        }

        if (userId != User.GetUserId()) return Forbid();

        var query = new GetWearStatisticsQuery(User.GetUserId(), range, customStart, customEnd);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpPost("outfit/{outfitId}")]
    public async Task<IActionResult> RecordOutfitWear(Guid outfitId, CancellationToken ct)
    {
        var command = new RecordOutfitWearCommand(User.GetUserId(), outfitId);
        var result = await _mediator.Send(command, ct);

        if (!result) return BadRequest("Record failed: Either the daily limit (10) was reached, or the outfit does not belong to you.");

        return Ok();
    }
}
