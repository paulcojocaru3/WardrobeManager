using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WardrobeManager.API.Extensions;
using WardrobeManager.Application.PlannedOutfits.Commands;
using WardrobeManager.Application.PlannedOutfits.Queries;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/planner-events")]
[Authorize]
public sealed class PlannerEventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlannerEventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{userId}/test-alert")]
    public async Task<ActionResult<WeatherAlertDto?>> GetTestAlert(Guid userId, CancellationToken ct)
    {
        if (userId != User.GetUserId()) return Forbid();
        var query = new GetTestAlertQuery(User.GetUserId());
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<GetPlannerEventsResult>> GetPlannerEvents(Guid userId, CancellationToken ct)
    {
        if (userId != User.GetUserId()) return Forbid();
        var query = new GetPlannerEventsQuery(User.GetUserId());
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("{userId}/archived")]
    public async Task<ActionResult<IEnumerable<PlannerEventDto>>> GetArchivedPlannerEvents(Guid userId, CancellationToken ct)
    {
        if (userId != User.GetUserId()) return Forbid();
        var query = new GetArchivedPlannerEventsQuery(User.GetUserId());
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    public record CreatePlannerEventRequest(Guid UserId, string Name, string Type, string Location, DateTime StartDate, DateTime EndDate, List<string>? PreferredStyles = null);

    [HttpPost]
    public async Task<ActionResult<Guid>> CreatePlannerEvent([FromBody] CreatePlannerEventRequest request, CancellationToken ct)
    {
        var preferredStyles = request.PreferredStyles;
        if (preferredStyles == null)
        {
            preferredStyles = new List<string>();
        }

        var command = new CreatePlannerEventCommand(User.GetUserId(), request.Name, request.Type, request.Location, request.StartDate, request.EndDate, preferredStyles);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    public record UpdatePlannerEventRequest(Guid UserId, string Name, string Type, string Location, DateTime StartDate, DateTime EndDate, List<string>? PreferredStyles = null);

    [HttpPut("{plannerEventId}")]
    public async Task<IActionResult> UpdatePlannerEvent(Guid plannerEventId, [FromBody] UpdatePlannerEventRequest request, CancellationToken ct)
    {
        var preferredStyles = request.PreferredStyles;
        if (preferredStyles == null)
        {
            preferredStyles = new List<string>();
        }

        var command = new UpdatePlannerEventCommand(User.GetUserId(), plannerEventId, request.Name, request.Type, request.Location, request.StartDate, request.EndDate, preferredStyles);
        var result = await _mediator.Send(command, ct);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{userId}/{plannerEventId}")]
    public async Task<IActionResult> DeletePlannerEvent(Guid userId, Guid plannerEventId, CancellationToken ct)
    {
        var command = new DeletePlannerEventCommand(User.GetUserId(), plannerEventId);
        var result = await _mediator.Send(command, ct);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("{plannerEventId}/archive")]
    public async Task<IActionResult> ArchivePlannerEvent(Guid plannerEventId, [FromQuery] Guid userId, CancellationToken ct)
    {
        var command = new ArchivePlannerEventCommand(User.GetUserId(), plannerEventId);
        var result = await _mediator.Send(command, ct);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    public record AddEventItineraryRequest(Guid UserId, Guid OutfitId, DateTime Date, string Moment);

    [HttpPost("{plannerEventId}/itineraries")]
    public async Task<ActionResult<Guid>> AddEventItinerary(Guid plannerEventId, [FromBody] AddEventItineraryRequest request, CancellationToken ct)
    {
        var command = new AddEventItineraryCommand(User.GetUserId(), plannerEventId, request.OutfitId, request.Date, request.Moment);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpDelete("{userId}/{plannerEventId}/itineraries/{itineraryId}")]
    public async Task<IActionResult> DeleteEventItinerary(Guid userId, Guid plannerEventId, Guid itineraryId, CancellationToken ct)
    {
        var command = new DeleteEventItineraryCommand(User.GetUserId(), plannerEventId, itineraryId);
        var result = await _mediator.Send(command, ct);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    public record GenerateEventOutfitsRequest(Guid UserId);

    [HttpPost("{plannerEventId}/generate-outfits")]
    [ProducesResponseType(typeof(GenerateEventOutfitsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<GenerateEventOutfitsResult>> GenerateEventOutfits(Guid plannerEventId, [FromBody] GenerateEventOutfitsRequest request, CancellationToken ct)
    {
        var command = new GenerateEventOutfitsCommand(User.GetUserId(), plannerEventId);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("{plannerEventId}/itineraries/{itineraryId}/regenerate")]
    public async Task<IActionResult> RegenerateEventItinerary(Guid plannerEventId, Guid itineraryId, [FromBody] GenerateEventOutfitsRequest request, CancellationToken ct)
    {
        var command = new RegenerateEventItineraryOutfitCommand(User.GetUserId(), plannerEventId, itineraryId);
        var result = await _mediator.Send(command, ct);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    public record UpdateEventItineraryRequest(Guid UserId, Guid OutfitId, DateTime Date, string Moment);

    [HttpPut("{plannerEventId}/itineraries/{itineraryId}")]
    public async Task<IActionResult> UpdateEventItinerary(Guid plannerEventId, Guid itineraryId, [FromBody] UpdateEventItineraryRequest request, CancellationToken ct)
    {
        var command = new UpdateEventItineraryCommand(User.GetUserId(), plannerEventId, itineraryId, request.OutfitId, request.Date, request.Moment);
        var result = await _mediator.Send(command, ct);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
