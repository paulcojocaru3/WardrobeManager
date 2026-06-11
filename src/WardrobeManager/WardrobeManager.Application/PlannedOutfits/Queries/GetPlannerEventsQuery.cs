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

        foreach (var plannerEvent in plannerEvents.ToList())
        {
            // Auto-archive if EndDate is in the past and it's still Active
            if (plannerEvent.Status == "Active" && plannerEvent.EndDate < now.Date)
            {
                plannerEvent.Status = "Archived";
                plannerEvent.ArchivedAt = now;
                await _plannerEventRepository.UpdateAsync(plannerEvent, cancellationToken);            }
            // Auto-delete if Archived more than 30 days ago
            else if (plannerEvent.Status == "Archived" && plannerEvent.ArchivedAt.HasValue && (now - plannerEvent.ArchivedAt.Value).TotalDays > 30)
            {
                await _plannerEventRepository.DeleteAsync(plannerEvent, cancellationToken);
                plannerEvents.Remove(plannerEvent);            }
        }

        // Filter to return only active events
        var activeEvents = plannerEvents.Where(p => p.Status == "Active").ToList();

        var dtos = activeEvents.Select(p => new PlannerEventDto
        {
            Id = p.Id,
            Name = p.Name,
            Type = p.Type,
            Location = p.Location,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Status = p.Status,
            ArchivedAt = p.ArchivedAt,
            PreferredStyles = p.PreferredStyles ?? new List<string>(),
            Itineraries = p.Itineraries.Select(i => new EventItineraryDto
            {
                Id = i.Id,
                OutfitId = i.OutfitId,
                Date = i.Date,
                Moment = i.Moment,
                StoredTemperature = i.StoredTemperature,
                Outfit = new OutfitDto(
                    i.Outfit.Id,
                    i.Outfit.Name,
                    i.Outfit.IsAiGenerated,
                    i.Outfit.IsFavorite,
                    i.Outfit.Tags,
                    i.Outfit.CreatedAt,
                    i.Outfit.Items.Select(item => new ClothingItemDto(
                        item.Id,
                        item.Name,
                        item.Type,
                        item.SubType,
                        item.Color,
                        item.Gender,
                        item.Season,
                        item.Usage,
                        item.ProcessedImageUrl,
                        item.CreatedAt
                    )).ToList()
                )
            }).ToList()
        }).ToList();

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