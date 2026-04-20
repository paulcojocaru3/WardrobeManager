using MediatR;
using Microsoft.AspNetCore.Mvc;
using WardrobeManager.Application.Clothing.Queries;
using WardrobeManager.Application.Outfits.Commands;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/wear-events")]
public class WearEventsController : ControllerBase
{
    private static readonly HashSet<string> SupportedRanges = new(StringComparer.OrdinalIgnoreCase)
    {
        "7d",
        "30d",
        "90d",
        "1y",
        "custom"
    };

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
        [FromQuery] DateTime? customEnd = null)
    {
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

        var statsWindow = ResolveStatsWindow(range, customStart, customEnd);
        if (!statsWindow.IsValid)
        {
            return BadRequest(statsWindow.Error);
        }

        var query = new GetWearStatisticsQuery(userId, statsWindow.StartUtc, statsWindow.EndUtc);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    private static StatsWindowResolution ResolveStatsWindow(string? range, DateTime? customStart, DateTime? customEnd)
    {
        var normalizedRange = string.IsNullOrWhiteSpace(range)
            ? null
            : range.Trim().ToLowerInvariant();

        if (normalizedRange is not null && !SupportedRanges.Contains(normalizedRange))
        {
            return StatsWindowResolution.Invalid($"Invalid range '{range}'. Allowed values: 7d, 30d, 90d, 1y, custom.");
        }

        var hasCustomStart = customStart.HasValue;
        var hasCustomEnd = customEnd.HasValue;
        var hasAnyCustomDate = hasCustomStart || hasCustomEnd;

        if (!hasAnyCustomDate && normalizedRange is null)
        {
            return StatsWindowResolution.Empty();
        }

        if (hasAnyCustomDate)
        {
            if (!hasCustomStart || !hasCustomEnd)
            {
                return StatsWindowResolution.Invalid("Both customStart and customEnd must be provided.");
            }

            if (normalizedRange is not null && normalizedRange != "custom")
            {
                return StatsWindowResolution.Invalid("When customStart/customEnd are provided, range must be omitted or set to 'custom'.");
            }

            var startUtc = NormalizeAsUtc(customStart.Value).Date;
            var endUtc = NormalizeAsUtc(customEnd.Value).Date.AddDays(1).AddTicks(-1);

            if (endUtc < startUtc)
            {
                return StatsWindowResolution.Invalid("customEnd must be greater than or equal to customStart.");
            }

            return StatsWindowResolution.Valid(startUtc, endUtc);
        }

        if (normalizedRange == "custom")
        {
            return StatsWindowResolution.Invalid("Range 'custom' requires both customStart and customEnd query parameters.");
        }

        var endDate = DateTime.UtcNow;
        var startDate = normalizedRange switch
        {
            "7d" => endDate.AddDays(-7),
            "30d" => endDate.AddDays(-30),
            "90d" => endDate.AddDays(-90),
            "1y" => endDate.AddYears(-1),
            _ => (DateTime?)null
        };

        if (!startDate.HasValue)
        {
            return StatsWindowResolution.Empty();
        }

        return StatsWindowResolution.Valid(startDate.Value, endDate);
    }

    private static DateTime NormalizeAsUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private sealed record StatsWindowResolution(bool IsValid, DateTime? StartUtc, DateTime? EndUtc, string? Error)
    {
        public static StatsWindowResolution Empty() => new(true, null, null, null);

        public static StatsWindowResolution Valid(DateTime startUtc, DateTime endUtc) => new(true, startUtc, endUtc, null);

        public static StatsWindowResolution Invalid(string error) => new(false, null, null, error);
    }

    public record RecordWearRequest(Guid UserId);

    [HttpPost("outfit/{outfitId}")]
    public async Task<IActionResult> RecordOutfitWear(Guid outfitId, [FromBody] RecordWearRequest request)
    {
        var command = new RecordOutfitWearCommand(request.UserId, outfitId);
        // We'll change the command return type to a more descriptive result later if needed,
        // but for now let's ensure the logic in handler is solid.
        var result = await _mediator.Send(command);

        if (!result) return BadRequest("Record failed: Either the daily limit (10) was reached, or the outfit does not belong to you.");
        
        return Ok();
    }
}
