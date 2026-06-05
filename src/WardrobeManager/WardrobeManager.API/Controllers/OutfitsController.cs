using MediatR;
using Microsoft.AspNetCore.Mvc;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Commands;
using WardrobeManager.Application.Outfits.Queries;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OutfitsController(IMediator mediator) : ControllerBase
{
    [HttpGet("weather/{city}")]
    public async Task<IActionResult> GetWeather(string city)
    {
        var weatherService = HttpContext.RequestServices.GetRequiredService<IWeatherService>();
        var weather = await weatherService.GetCurrentWeatherAsync(city);
        return Ok(weather);
    }

    [HttpGet("weather/{city}/forecast")]
    public async Task<IActionResult> GetForecast(string city, [FromQuery] int days = 14, [FromQuery] DateTime? startDate = null)
    {
        var weatherService = HttpContext.RequestServices.GetRequiredService<IWeatherService>();
        var forecasts = await weatherService.GetForecastAsync(city, days, startDate);
        return Ok(new { forecasts });
    }

    [HttpGet("cities/search")]
    public async Task<IActionResult> SearchCities([FromQuery] string query)
    {
        var weatherService = HttpContext.RequestServices.GetRequiredService<IWeatherService>();
        var cities = await weatherService.SearchCitiesAsync(query);
        return Ok(cities);
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateOutfit([FromBody] GenerateOutfitCommand command)
    {
        var outfit = await mediator.Send(command);
        return Ok(outfit);
    }

    [HttpPost("generate-ai")]
    public async Task<IActionResult> GenerateAiOutfit([FromBody] GenerateAiOutfitCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("generate-from-prompt")]
    public async Task<IActionResult> GenerateFromPrompt([FromBody] GenerateOutfitFromPromptCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOutfit([FromBody] CreateOutfitCommand command)
    {
        var id = await mediator.Send(command);
        return Ok(id);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserOutfits(Guid userId)
    {
        var outfits = await mediator.Send(new GetOutfitsQuery(userId));
        return Ok(outfits);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOutfit(Guid id, [FromBody] UpdateOutfitCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        await mediator.Send(command);
        return Ok();
    }

    [HttpPut("{id}/favorite")]
    public async Task<IActionResult> ToggleFavorite(Guid id)
    {
        var result = await mediator.Send(new ToggleOutfitFavoriteCommand(id));
        return Ok(new { isFavorite = result });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOutfit(Guid id)
    {
        await mediator.Send(new DeleteOutfitCommand(id));
        return NoContent();
    }

    [HttpPost("parse-prompt")]
    public async Task<IActionResult> ParsePrompt([FromBody] ParsePromptRequest request)
    {
        var mlService = HttpContext.RequestServices.GetRequiredService<IMlService>();
        var (style, confidence, city) = await mlService.ParsePromptAsync(request.Prompt);
        return Ok(new { style, styleConfidence = confidence, city });
    }

    public record ParsePromptRequest(string Prompt);
}
