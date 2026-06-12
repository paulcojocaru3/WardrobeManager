using MediatR;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public record RegenerateEventItineraryOutfitCommand(Guid UserId, Guid PlannerEventId, Guid ItineraryId) : IRequest<bool>;

public sealed class RegenerateEventItineraryOutfitCommandHandler : IRequestHandler<RegenerateEventItineraryOutfitCommand, bool>
{
    private readonly IPlannerEventRepository _plannerEventRepository;
    private readonly IOutfitRepository _outfitRepository;
    private readonly IOutfitGenerator _outfitGenerator;
    private readonly IClothingRepository _clothingRepository;
    private readonly IEventOutfitPlanningService _eventOutfitPlanningService;
    private readonly IWeatherService _weatherService;
    private readonly ILogger<RegenerateEventItineraryOutfitCommandHandler> _logger;

    public RegenerateEventItineraryOutfitCommandHandler(
        IPlannerEventRepository plannerEventRepository,
        IOutfitRepository outfitRepository,
        IOutfitGenerator outfitGenerator,
        IClothingRepository clothingRepository,
        IEventOutfitPlanningService eventOutfitPlanningService,
        IWeatherService weatherService,
        ILogger<RegenerateEventItineraryOutfitCommandHandler> logger)
    {
        _plannerEventRepository = plannerEventRepository;
        _outfitRepository = outfitRepository;
        _outfitGenerator = outfitGenerator;
        _clothingRepository = clothingRepository;
        _eventOutfitPlanningService = eventOutfitPlanningService;
        _weatherService = weatherService;
        _logger = logger;
    }

    public async Task<bool> Handle(RegenerateEventItineraryOutfitCommand request, CancellationToken ct)
    {
        var plannerEvent = await _plannerEventRepository.GetByIdAsync(request.PlannerEventId, ct);
        if (plannerEvent == null || plannerEvent.UserId != request.UserId)
        {
            return false;
        }

        var itinerary = plannerEvent.Itineraries.FirstOrDefault(i => i.Id == request.ItineraryId);
        if (itinerary == null)
        {
            return false;
        }

        var dayIndex = (itinerary.Date.Date - plannerEvent.StartDate.Date).Days;
        if (dayIndex < 0)
        {
            dayIndex = 0;
        }

        WeatherData? weather = null;
        try
        {
            var totalDays = (plannerEvent.EndDate.Date - plannerEvent.StartDate.Date).Days + 1;
            var forecast = await _weatherService.GetForecastAsync(plannerEvent.Location, Math.Max(totalDays, dayIndex + 1), plannerEvent.StartDate.Date, ct);
            if (dayIndex < forecast.Count)
            {
                var dayForecast = forecast[dayIndex];
                weather = new WeatherData(dayForecast.Temperature, dayForecast.Condition, dayForecast.SeasonSuggestion);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Forecast unavailable for {Location}; regenerating without weather.", plannerEvent.Location);
        }

        var (style, _) = _eventOutfitPlanningService.ResolveDayPlan(plannerEvent.Type, dayIndex, weather, itinerary.Moment, plannerEvent.PreferredStyles);

        List<Guid> excludedItemIds;
        if (itinerary.Outfit != null)
        {
            excludedItemIds = itinerary.Outfit.Items.Select(i => i.Id).ToList();
        }
        else
        {
            excludedItemIds = new List<Guid>();
        }
        var startItem = await _eventOutfitPlanningService.SelectStartItemAsync(request.UserId, style, weather, excludedItemIds, ct);
        if (startItem == null || startItem.Embedding == null)
        {
            return false;
        }

        var aiResult = await _outfitGenerator.GenerateAiOutfitAsync(
            request.UserId,
            startItem.Id,
            new OutfitGenerationOptions
            {
                Threshold = 0.4,
                Weather = weather,
                Style = style
            },
            ct);

        var itemIds = aiResult.SelectedItems.Select(si => si.Id).ToList();
        var items = await _clothingRepository.GetByIdsAsync(itemIds, ct);

        if (items.Count == 0)
        {
            return false;
        }

        var newOutfit = new Outfit
        {
            UserId = request.UserId,
            Name = $"{plannerEvent.Name} - {itinerary.Date:yyyy-MM-dd} ({style})",
            Items = items,
            IsAiGenerated = true,
            IsEventExclusive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _outfitRepository.AddAsync(newOutfit, ct);

        itinerary.OutfitId = newOutfit.Id;
        itinerary.StoredTemperature = weather?.Temperature;
        await _plannerEventRepository.UpdateItineraryAsync(itinerary, ct);

        return true;
    }
}
