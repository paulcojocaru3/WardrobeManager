using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WardrobeManager.API.Extensions;
using WardrobeManager.Application.Outfits.Commands;
using WardrobeManager.Application.Outfits.Queries;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class OutfitsController(IMediator mediator) : ControllerBase
{
    [HttpGet("weather/{city}")]
    public async Task<IActionResult> GetWeather(string city, CancellationToken ct)
    {
        var weather = await mediator.Send(new GetCurrentWeatherQuery(city), ct);
        return Ok(weather);
    }

    [HttpGet("weather/{city}/forecast")]
    public async Task<IActionResult> GetForecast(string city, [FromQuery] int days = 14, [FromQuery] DateTime? startDate = null, CancellationToken ct = default)
    {
        var forecasts = await mediator.Send(new GetWeatherForecastQuery(city, days, startDate), ct);
        return Ok(new { forecasts });
    }

    [HttpGet("cities/search")]
    public async Task<IActionResult> SearchCities([FromQuery] string query, CancellationToken ct)
    {
        var cities = await mediator.Send(new SearchCitiesQuery(query), ct);
        return Ok(cities);
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateOutfit([FromBody] GenerateOutfitCommand command, CancellationToken ct)
    {
        var outfit = await mediator.Send(command with { UserId = User.GetUserId() }, ct);
        return Ok(outfit);
    }

    [HttpPost("generate-ai")]
    public async Task<IActionResult> GenerateAiOutfit([FromBody] GenerateAiOutfitCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command with { UserId = User.GetUserId() }, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOutfit([FromBody] CreateOutfitCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command with { UserId = User.GetUserId() }, ct);
        return Ok(id);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserOutfits(Guid userId, CancellationToken ct)
    {
        if (userId != User.GetUserId()) return Forbid();
        var outfits = await mediator.Send(new GetOutfitsQuery(User.GetUserId()), ct);
        return Ok(outfits);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOutfit(Guid id, [FromBody] UpdateOutfitCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        var result = await mediator.Send(command with { UserId = User.GetUserId() }, ct);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpPut("{id}/favorite")]
    public async Task<IActionResult> ToggleFavorite(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ToggleOutfitFavoriteCommand(User.GetUserId(), id), ct);
        if (result == null) return NotFound();
        return Ok(new { isFavorite = result });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOutfit(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteOutfitCommand(User.GetUserId(), id), ct);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPost("feedback")]
    public async Task<IActionResult> RecordFeedback([FromBody] OutfitFeedbackRequest request, CancellationToken ct)
    {
        await mediator.Send(new RecordOutfitFeedbackCommand(User.GetUserId(), request.GenerationId, request.Items), ct);
        return Ok();
    }

    // natural-language "why this works" notes for an already-generated outfit (grounded in its facts).
    [HttpPost("styling-notes")]
    public async Task<IActionResult> GetStylingNotes([FromBody] StylingNotesRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new ExplainOutfitQuery(User.GetUserId(), request.ItemIds, request.Style, request.Occasion, request.City, request.Tradeoffs), ct);
        return Ok(result);
    }

    // richer weather-aware insight (headline + per-item notes + weather advice) for the daily outfit.
    [HttpPost("insight")]
    public async Task<IActionResult> GetOutfitInsight([FromBody] StylingNotesRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new OutfitInsightQuery(User.GetUserId(), request.ItemIds, request.Style, request.Occasion, request.City, request.Tradeoffs), ct);
        return Ok(result);
    }

    [HttpGet("learned-profile")]
    public async Task<IActionResult> GetLearnedProfile(CancellationToken ct)
    {
        var profile = await mediator.Send(new GetLearnedProfileQuery(User.GetUserId()), ct);
        return Ok(profile);
    }

    public record OutfitFeedbackRequest([property: JsonRequired] Guid GenerationId, List<OutfitFeedbackItem> Items);
    public record StylingNotesRequest(
        List<Guid> ItemIds, string? Style, string? Occasion, string? City, List<string>? Tradeoffs);
}
