using MediatR;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing.Queries;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Application.PlannedOutfits.Commands;

namespace WardrobeManager.Application.PlannedOutfits.Queries;

public record GetPlannerEventsResult(IEnumerable<PlannerEventDto> PlannerEvents, WeatherAlertDto? WeatherAlert);

public record GetPlannerEventsQuery(Guid UserId) : IRequest<GetPlannerEventsResult>;

public sealed class GetPlannerEventsQueryHandler : IRequestHandler<GetPlannerEventsQuery, GetPlannerEventsResult>
{
    private readonly IPlannerEventRepository _plannerEventRepository;
    private readonly IWeatherService _weatherService;
    private readonly ILogger<GetPlannerEventsQueryHandler> _logger;

    public GetPlannerEventsQueryHandler(IPlannerEventRepository plannerEventRepository, IWeatherService weatherService, ILogger<GetPlannerEventsQueryHandler> logger)
    {
        _plannerEventRepository = plannerEventRepository;
        _weatherService = weatherService;
        _logger = logger;
    }

    public async Task<GetPlannerEventsResult> Handle(GetPlannerEventsQuery request, CancellationToken cancellationToken)
    {
        var plannerEvents = (await _plannerEventRepository.GetByUserIdAsync(request.UserId, cancellationToken)).ToList();

        var now = DateTime.UtcNow;

        await PlannerEventProjection.ApplyLifecycleTransitionsAsync(plannerEvents, _plannerEventRepository, now, cancellationToken);

        // Filter to return only active events
        var activeEvents = plannerEvents.Where(p => p.Status == "Active").ToList();

        var dtos = activeEvents.Select(PlannerEventProjection.ToDto).ToList();

        // Check for weather drift
        WeatherAlertDto? weatherAlert = null;
        
        // Find the first active event that has upcoming days with stored temperatures
        var upcomingEvent = activeEvents
            .Where(e => e.EndDate.Date >= now.Date && e.Itineraries.Any(i => i.Date.Date >= now.Date && i.StoredTemperature.HasValue))
            .OrderBy(e => e.StartDate)
            .FirstOrDefault();

        if (upcomingEvent != null)
        {
            var totalDays = (int)(upcomingEvent.EndDate.Date - upcomingEvent.StartDate.Date).TotalDays + 1;
            try
            {
                var forecast = await _weatherService.GetForecastAsync(upcomingEvent.Location, totalDays, upcomingEvent.StartDate.Date, cancellationToken);
                
                foreach (var itinerary in upcomingEvent.Itineraries.Where(i => i.Date.Date >= now.Date && i.StoredTemperature.HasValue))
                {
                    var dayForecast = forecast.FirstOrDefault(f => f.Date.Date == itinerary.Date.Date);
                    if (dayForecast != null)
                    {
                        var tempDelta = Math.Abs(dayForecast.Temperature - itinerary.StoredTemperature!.Value);
                        if (tempDelta >= 5f) // 5 degrees drift
                        {
                            weatherAlert = new WeatherAlertDto(
                                IsAvailable: true,
                                IsSignificantChange: true,
                                TemperatureDelta: dayForecast.Temperature - itinerary.StoredTemperature.Value,
                                StoredForecast: new WeatherDataDto(itinerary.StoredTemperature.Value, "Unknown", "Unknown", itinerary.Date),
                                CurrentWeather: new WeatherDataDto(dayForecast.Temperature, dayForecast.Condition, dayForecast.SeasonSuggestion, itinerary.Date),
                                EventName: upcomingEvent.Name,
                                EventDate: itinerary.Date,
                                PlannerEventId: upcomingEvent.Id
                            );
                            break; // Only one alert per event (Variant A)
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Weather drift check failed; skipping alert.");
            }
        }

        return new GetPlannerEventsResult(dtos, weatherAlert);
    }
}