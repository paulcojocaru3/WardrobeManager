using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public class GenerateEventOutfitsCommandHandler : IRequestHandler<GenerateEventOutfitsCommand, GenerateEventOutfitsResult>
{
    private readonly IPlannerEventRepository _plannerEventRepository;
    private readonly IOutfitRepository _outfitRepository;
    private readonly IClothingRepository _clothingRepository;
    private readonly IOutfitGenerator _outfitGenerator;
    private readonly IWeatherService _weatherService;
    private readonly IEventOutfitPlanningService _eventOutfitPlanningService;

    public GenerateEventOutfitsCommandHandler(
        IPlannerEventRepository plannerEventRepository,
        IOutfitRepository outfitRepository,
        IClothingRepository clothingRepository,
        IOutfitGenerator outfitGenerator,
        IWeatherService weatherService,
        IEventOutfitPlanningService eventOutfitPlanningService)
    {
        _plannerEventRepository = plannerEventRepository;
        _outfitRepository = outfitRepository;
        _clothingRepository = clothingRepository;
        _outfitGenerator = outfitGenerator;
        _weatherService = weatherService;
        _eventOutfitPlanningService = eventOutfitPlanningService;
    }

    public async Task<GenerateEventOutfitsResult> Handle(GenerateEventOutfitsCommand request, CancellationToken ct)
    {
        var plannerEvent = await _plannerEventRepository.GetByIdAsync(request.PlannerEventId, ct);
        if (plannerEvent == null || plannerEvent.UserId != request.UserId)
        {
            throw new Exception("Planner event not found or does not belong to user.");
        }

        var startDate = plannerEvent.StartDate.Date;
        var endDate = plannerEvent.EndDate.Date;
        var totalDays = (int)(endDate - startDate).TotalDays + 1;

        if (totalDays <= 0 || totalDays > 30)
        {
            throw new Exception("Invalid date range. Must be between 1 and 30 days.");
        }

        var generatedDays = new List<GeneratedDayDto>();
        var location = plannerEvent.Location;
        var usedStartItemIds = new HashSet<Guid>();

        // Get forecast for all days at once
        List<DailyForecast> forecast = new();
        try 
        { 
            forecast = await _weatherService.GetForecastAsync(location, totalDays, startDate, ct); 
        }
        catch { }

        for (int i = 0; i < totalDays; i++)
        {
            var currentDate = startDate.AddDays(i);

            // Get weather for this specific day from forecast
            DailyForecast? dayForecast = i < forecast.Count ? forecast[i] : null;
            var weather = dayForecast != null 
                ? new WeatherData(dayForecast.Temperature, dayForecast.Condition, dayForecast.SeasonSuggestion)
                : null;

            var (style, moment) = _eventOutfitPlanningService.ResolveDayPlan(plannerEvent.Type, i, weather);

            // Generate outfit
            var outfitResult = await GenerateOutfitForDay(request.UserId, style, weather, usedStartItemIds, ct);
            
            if (outfitResult != null)
            {
                // Load full clothing items from database
                var items = new List<ClothingItem>();
                foreach (var si in outfitResult.SelectedItems)
                {
                    var item = await _clothingRepository.GetByIdAsync(si.Id, ct);
                    if (item != null) items.Add(item);
                }

                // Save outfit
                var outfit = new Outfit
                {
                    UserId = request.UserId,
                    Name = $"{plannerEvent.Name} - Day {i + 1} ({style})",
                    Items = items,
                    IsAiGenerated = true,
                    IsEventExclusive = true
                };

                await _outfitRepository.AddAsync(outfit, ct);

                // Add to itinerary
                var itinerary = new EventItinerary
                {
                    PlannerEventId = plannerEvent.Id,
                    OutfitId = outfit.Id,
                    Date = currentDate,
                    Moment = moment
                };

                await _plannerEventRepository.AddItineraryAsync(itinerary, ct);

                generatedDays.Add(new GeneratedDayDto(
                    currentDate,
                    style,
                    weather?.Condition ?? "Unknown",
                    outfit.Id,
                    outfit.Name
                ));

                usedStartItemIds.UnionWith(items.Select(i => i.Id));
            }
        }

        return new GenerateEventOutfitsResult(totalDays, generatedDays.Count, generatedDays);
    }

    private async Task<Application.Outfits.Queries.AiGeneratedOutfitDto?> GenerateOutfitForDay(
        Guid userId, 
        string style, 
        WeatherData? weather,
        IReadOnlyCollection<Guid>? excludedItemIds,
        CancellationToken ct)
    {
        var startItem = await _eventOutfitPlanningService.SelectStartItemAsync(userId, style, weather, excludedItemIds, ct);
        if (startItem == null)
        {
            return null;
        }

        if (startItem.Embedding == null)
        {
            return null;
        }

        try
        {
            return await _outfitGenerator.GenerateAiOutfitAsync(
                userId,
                startItem.Id,
                threshold: 0.4,
                weatherData: weather,
                style: style,
                ct: ct);
        }
        catch
        {
            return null;
        }
    }
}
