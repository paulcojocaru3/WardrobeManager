using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WardrobeManager.API.Extensions;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Commands;
using WardrobeManager.Application.Outfits.Learning;
using WardrobeManager.Application.Outfits.Queries;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class OutfitsController(
    IMediator mediator,
    IWeatherService weatherService,
    IMlService mlService,
    IWeightLearningService weightLearningService) : ControllerBase
{
    [HttpGet("weather/{city}")]
    public async Task<IActionResult> GetWeather(string city, CancellationToken ct)
    {
        var weather = await weatherService.GetCurrentWeatherAsync(city, ct);
        return Ok(weather);
    }

    [HttpGet("weather/{city}/forecast")]
    public async Task<IActionResult> GetForecast(string city, [FromQuery] int days = 14, [FromQuery] DateTime? startDate = null, CancellationToken ct = default)
    {
        var forecasts = await weatherService.GetForecastAsync(city, days, startDate, ct);
        return Ok(new { forecasts });
    }

    [HttpGet("cities/search")]
    public async Task<IActionResult> SearchCities([FromQuery] string query, CancellationToken ct)
    {
        var cities = await weatherService.SearchCitiesAsync(query, ct);
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

    [HttpPost("generate-from-prompt")]
    public async Task<IActionResult> GenerateFromPrompt([FromBody] GenerateOutfitFromPromptCommand command, CancellationToken ct)
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
        await mediator.Send(command, ct);
        return Ok();
    }

    [HttpPut("{id}/favorite")]
    public async Task<IActionResult> ToggleFavorite(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ToggleOutfitFavoriteCommand(id), ct);
        return Ok(new { isFavorite = result });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOutfit(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteOutfitCommand(id), ct);
        return NoContent();
    }

    [HttpPost("parse-prompt")]
    public async Task<IActionResult> ParsePrompt([FromBody] ParsePromptRequest request, CancellationToken ct)
    {
        var (style, confidence, city) = await mlService.ParsePromptAsync(request.Prompt, ct);
        return Ok(new { style, styleConfidence = confidence, city });
    }

    [HttpPost("feedback")]
    public async Task<IActionResult> RecordFeedback([FromBody] OutfitFeedbackRequest request, CancellationToken ct)
    {
        await mediator.Send(new RecordOutfitFeedbackCommand(User.GetUserId(), request.GenerationId, request.Items), ct);
        return Ok();
    }

    // Manual retrain for the current user (used by the eval script / demos).
    [HttpPost("retrain-weights")]
    public async Task<IActionResult> RetrainWeights(CancellationToken ct)
    {
        await weightLearningService.RetrainAsync(User.GetUserId(), ct);
        return Ok();
    }

    public record ParsePromptRequest(string Prompt);
    public record OutfitFeedbackRequest(Guid GenerationId, List<OutfitFeedbackItem> Items);
}
