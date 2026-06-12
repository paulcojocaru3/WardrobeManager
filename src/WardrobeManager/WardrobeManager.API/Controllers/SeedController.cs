using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WardrobeManager.API.Extensions;
using WardrobeManager.Application.Seeding.Commands;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/seed")]
[Authorize]
public sealed class SeedController(IMediator mediator) : ControllerBase
{
    [HttpPost("backfill-subtypes/{userId}")]
    public async Task<IActionResult> BackfillSubTypes(Guid userId, CancellationToken ct)
    {
        if (userId != User.GetUserId()) return Forbid();

        var count = await mediator.Send(new BackfillSubTypesCommand(userId), ct);
        return Ok(count == 0 ? "No items need backfill." : $"Backfilled SubType for {count} items.");
    }

    [HttpPost("wear-events/{userId}")]
    public async Task<IActionResult> SeedWearEvents(Guid userId, CancellationToken ct)
    {
        if (userId != User.GetUserId()) return Forbid();

        var result = await mediator.Send(new SeedWearEventsCommand(userId), ct);
        return Ok($"Added {result.EventsAdded} wear events across 6 months for user {result.Username}");
    }
}
