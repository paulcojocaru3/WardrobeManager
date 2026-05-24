using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WardrobeManager.Application.PlannedOutfits.Commands;
using WardrobeManager.Application.PlannedOutfits.Queries;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/planner-events")]
public class PlannerEventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlannerEventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{userId}/test-alert")]
    public async Task<ActionResult<WeatherAlertDto?>> GetTestAlert(Guid userId)
    {
        var query = new GetTestAlertQuery(userId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<GetPlannerEventsResult>> GetPlannerEvents(Guid userId)
    {
        var query = new GetPlannerEventsQuery(userId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{userId}/archived")]
    public async Task<ActionResult<IEnumerable<PlannerEventDto>>> GetArchivedPlannerEvents(Guid userId)
    {
        var query = new GetArchivedPlannerEventsQuery(userId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    public record CreatePlannerEventRequest(Guid UserId, string Name, string Type, string Location, DateTime StartDate, DateTime EndDate, List<string>? PreferredStyles = null);

    [HttpPost]
    public async Task<ActionResult<Guid>> CreatePlannerEvent([FromBody] CreatePlannerEventRequest request)
    {
        var command = new CreatePlannerEventCommand(request.UserId, request.Name, request.Type, request.Location, request.StartDate, request.EndDate, request.PreferredStyles ?? new List<string>());
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    public record UpdatePlannerEventRequest(Guid UserId, string Name, string Type, string Location, DateTime StartDate, DateTime EndDate, List<string>? PreferredStyles = null);

    [HttpPut("{plannerEventId}")]
    public async Task<IActionResult> UpdatePlannerEvent(Guid plannerEventId, [FromBody] UpdatePlannerEventRequest request)
    {
        var command = new UpdatePlannerEventCommand(request.UserId, plannerEventId, request.Name, request.Type, request.Location, request.StartDate, request.EndDate, request.PreferredStyles ?? new List<string>());
        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{userId}/{plannerEventId}")]
    public async Task<IActionResult> DeletePlannerEvent(Guid userId, Guid plannerEventId)
    {
        var command = new DeletePlannerEventCommand(userId, plannerEventId);
        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("{plannerEventId}/archive")]
    public async Task<IActionResult> ArchivePlannerEvent(Guid plannerEventId, [FromQuery] Guid userId)
    {
        var command = new ArchivePlannerEventCommand(userId, plannerEventId);
        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    public record AddEventItineraryRequest(Guid UserId, Guid OutfitId, DateTime Date, string Moment);

    [HttpPost("{plannerEventId}/itineraries")]
    public async Task<ActionResult<Guid>> AddEventItinerary(Guid plannerEventId, [FromBody] AddEventItineraryRequest request)
    {
        var command = new AddEventItineraryCommand(request.UserId, plannerEventId, request.OutfitId, request.Date, request.Moment);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{userId}/{plannerEventId}/itineraries/{itineraryId}")]
    public async Task<IActionResult> DeleteEventItinerary(Guid userId, Guid plannerEventId, Guid itineraryId)
    {
        var command = new DeleteEventItineraryCommand(userId, plannerEventId, itineraryId);
        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    public record GenerateEventOutfitsRequest(Guid UserId);

    [HttpPost("{plannerEventId}/generate-outfits")]
    [ProducesResponseType(typeof(GenerateEventOutfitsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<GenerateEventOutfitsResult>> GenerateEventOutfits(Guid plannerEventId, [FromBody] GenerateEventOutfitsRequest request)
    {
        var command = new GenerateEventOutfitsCommand(request.UserId, plannerEventId);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("{plannerEventId}/itineraries/{itineraryId}/regenerate")]
    public async Task<IActionResult> RegenerateEventItinerary(Guid plannerEventId, Guid itineraryId, [FromBody] GenerateEventOutfitsRequest request)
    {
        var command = new RegenerateEventItineraryOutfitCommand(request.UserId, plannerEventId, itineraryId);
        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    public record UpdateEventItineraryRequest(Guid UserId, Guid OutfitId, DateTime Date, string Moment);

    [HttpPut("{plannerEventId}/itineraries/{itineraryId}")]
    public async Task<IActionResult> UpdateEventItinerary(Guid plannerEventId, Guid itineraryId, [FromBody] UpdateEventItineraryRequest request)
    {
        var command = new UpdateEventItineraryCommand(request.UserId, plannerEventId, itineraryId, request.OutfitId, request.Date, request.Moment);
        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
