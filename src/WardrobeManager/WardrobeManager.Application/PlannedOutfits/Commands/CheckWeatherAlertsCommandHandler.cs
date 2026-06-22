using MediatR;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.PlannedOutfits;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public sealed class CheckWeatherAlertsCommandHandler(
    IPlannerEventRepository plannerEventRepository,
    IWeatherService weatherService,
    INotificationDispatcher notificationDispatcher,
    ILogger<CheckWeatherAlertsCommandHandler> logger,
    TimeProvider? clock = null)
    : IRequestHandler<CheckWeatherAlertsCommand, int>
{
    public async Task<int> Handle(CheckWeatherAlertsCommand request, CancellationToken ct)
    {
        var now = (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        var events = await plannerEventRepository.GetActiveWithUpcomingItinerariesAsync(now, ct);
        var alertsSent = 0;

        foreach (var plannerEvent in events)
        {
            if (await CheckEventAsync(plannerEvent, now, ct))
            {
                alertsSent++;
            }
        }

        return alertsSent;
    }

    private async Task<bool> CheckEventAsync(PlannerEvent plannerEvent, DateTime now, CancellationToken ct)
    {
        var totalDays = (int)(plannerEvent.EndDate.Date - plannerEvent.StartDate.Date).TotalDays + 1;

        List<DailyForecast> forecast;
        try
        {
            forecast = await weatherService.GetForecastAsync(
                plannerEvent.Location,
                totalDays,
                plannerEvent.StartDate.Date,
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Forecast fetch failed for event {EventId}.", plannerEvent.Id);
            return false;
        }

        foreach (var itinerary in plannerEvent.Itineraries.Where(i => i.Date.Date >= now.Date && i.StoredTemperature.HasValue))
        {
            var dayForecast = forecast.FirstOrDefault(f => f.Date.Date == itinerary.Date.Date);
            if (dayForecast == null)
            {
                continue;
            }

            var stored = new WeatherData(itinerary.StoredTemperature!.Value, "Unknown", "Unknown");
            var current = new WeatherData(dayForecast.Temperature, dayForecast.Condition, dayForecast.SeasonSuggestion);
            var (isSignificant, _) = EventOutfitPlanningService.CompareForecastToCurrentWeather(stored, current);
            if (!isSignificant)
            {
                continue;
            }

            await DispatchAlertAsync(plannerEvent, itinerary, dayForecast, ct);
            return true;
        }

        return false;
    }

    private async Task DispatchAlertAsync(
        PlannerEvent plannerEvent,
        EventItinerary itinerary,
        DailyForecast dayForecast,
        CancellationToken ct)
    {
        var signedDelta = dayForecast.Temperature - itinerary.StoredTemperature!.Value;
        var dedupKey = $"WeatherAlert:{plannerEvent.Id}:{itinerary.Date:yyyyMMdd}:{Math.Round(signedDelta)}";

        var payload = new WeatherAlertDto(
            IsAvailable: true,
            IsSignificantChange: true,
            TemperatureDelta: signedDelta,
            StoredForecast: new WeatherDataDto(itinerary.StoredTemperature.Value, "Unknown", "Unknown", itinerary.Date),
            CurrentWeather: new WeatherDataDto(dayForecast.Temperature, dayForecast.Condition, dayForecast.SeasonSuggestion, itinerary.Date),
            EventName: plannerEvent.Name,
            EventDate: itinerary.Date,
            PlannerEventId: plannerEvent.Id);

        var direction = signedDelta >= 0 ? "warmer" : "colder";
        var magnitude = Math.Abs((int)Math.Round(signedDelta));

        await notificationDispatcher.DispatchAsync(
            plannerEvent.UserId,
            "WeatherAlert",
            "Weather changed for your event",
            $"{plannerEvent.Name}: about {magnitude} degrees C {direction} than planned on {itinerary.Date:MMM d}.",
            payload,
            dedupKey,
            ct);
    }
}
