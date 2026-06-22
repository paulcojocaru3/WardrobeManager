using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

// shared forecast lookup for the itinerary commands (add/update), so the fetch-and-resolve block
internal static class EventForecastHelper
{
    public static async Task<float?> TryGetTemperatureAsync(
        IWeatherService weatherService,
        ILogger logger,
        PlannerEvent plannerEvent,
        DateTime targetDate,
        CancellationToken ct)
    {
        try
        {
            var totalDays = (int)(plannerEvent.EndDate.Date - plannerEvent.StartDate.Date).TotalDays + 1;
            var forecast = await weatherService.GetForecastAsync(plannerEvent.Location, totalDays, plannerEvent.StartDate.Date, ct);
            var dayForecast = forecast.FirstOrDefault(f => f.Date.Date == targetDate.Date);
            return dayForecast?.Temperature;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Forecast unavailable for {Location}; temperature not updated.", plannerEvent.Location);
            return null;
        }
    }
}
