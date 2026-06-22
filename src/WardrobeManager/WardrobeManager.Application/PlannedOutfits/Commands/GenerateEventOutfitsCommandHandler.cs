using MediatR;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Application.Outfits.Prompting;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Application.PlannedOutfits;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public sealed class GenerateEventOutfitsCommandHandler : IRequestHandler<GenerateEventOutfitsCommand, GenerateEventOutfitsResult>
{
    private readonly IPlannerEventRepository _plannerEventRepository;
    private readonly IOutfitRepository _outfitRepository;
    private readonly IClothingRepository _clothingRepository;
    private readonly IOutfitGenerator _outfitGenerator;
    private readonly IWeatherService _weatherService;
    private readonly IEventOutfitPlanningService _eventOutfitPlanningService;
    private readonly IStartItemSelector _startItemSelector;
    private readonly IUserRepository _userRepository;
    private readonly StylistOutfitComposer _composer;
    private readonly StylistCandidatePoolBuilder _poolBuilder;
    private readonly StylistSettings _stylistSettings;
    private readonly IOccasionFormalityRules _occasionFormalityRules;
    private readonly IOutfitFeedbackRepository _feedbackRepository;
    private readonly ILogger<GenerateEventOutfitsCommandHandler> _logger;
    private readonly TimeProvider _clock;

    public GenerateEventOutfitsCommandHandler(
        IPlannerEventRepository plannerEventRepository,
        IOutfitRepository outfitRepository,
        IClothingRepository clothingRepository,
        IOutfitGenerator outfitGenerator,
        IWeatherService weatherService,
        IEventOutfitPlanningService eventOutfitPlanningService,
        IStartItemSelector startItemSelector,
        IUserRepository userRepository,
        StylistOutfitComposer composer,
        StylistCandidatePoolBuilder poolBuilder,
        StylistSettings stylistSettings,
        IOccasionFormalityRules occasionFormalityRules,
        IOutfitFeedbackRepository feedbackRepository,
        ILogger<GenerateEventOutfitsCommandHandler> logger,
        TimeProvider? clock = null)
    {
        _plannerEventRepository = plannerEventRepository;
        _outfitRepository = outfitRepository;
        _clothingRepository = clothingRepository;
        _outfitGenerator = outfitGenerator;
        _weatherService = weatherService;
        _eventOutfitPlanningService = eventOutfitPlanningService;
        _startItemSelector = startItemSelector;
        _userRepository = userRepository;
        _composer = composer;
        _poolBuilder = poolBuilder;
        _stylistSettings = stylistSettings;
        _occasionFormalityRules = occasionFormalityRules;
        _feedbackRepository = feedbackRepository;
        _logger = logger;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<GenerateEventOutfitsResult> Handle(GenerateEventOutfitsCommand request, CancellationToken ct)
    {
        var plannerEvent = await _plannerEventRepository.GetByIdAsync(request.PlannerEventId, ct);
        if (plannerEvent == null || plannerEvent.UserId != request.UserId)
        {
            throw new KeyNotFoundException("Planner event not found or does not belong to user.");
        }

        var startDate = plannerEvent.StartDate.Date;
        var endDate = plannerEvent.EndDate.Date;
        var totalDays = (int)(endDate - startDate).TotalDays + 1;

        if (totalDays <= 0 || totalDays > 30)
        {
            throw new InvalidOperationException("Invalid date range. Must be between 1 and 30 days.");
        }

        var userClothes = await _clothingRepository.GetByUserIdAsync(request.UserId, ct);
        if (userClothes.Count < 5)
        {
            throw new InvalidOperationException("You need at least 5 items in your wardrobe to generate a trip itinerary.");
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        var useStylist = user?.UseGemmaStylistForOutfits == true && _stylistSettings.Enabled;

        var generatedDays = new List<GeneratedDayDto>();
        var location = plannerEvent.Location;
        var itemUsageByDate = EventReusePolicy.BuildUsageMap(plannerEvent.Itineraries);

        List<DailyForecast> forecast = new();
        try
        {
            forecast = await _weatherService.GetForecastAsync(location, totalDays, startDate, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Forecast unavailable for {Location}; generating without weather.", location);
        }

        WeatherAlertDto? weatherAlert = null;

        for (int i = 0; i < totalDays; i++)
        {
            var currentDate = startDate.AddDays(i);

            DailyForecast? dayForecast = i < forecast.Count ? forecast[i] : null;
            var weather = dayForecast != null
                ? new WeatherData(dayForecast.Temperature, dayForecast.Condition, dayForecast.SeasonSuggestion)
                : null;

            var existingItinerary = plannerEvent.Itineraries.FirstOrDefault(it => it.Date.Date == currentDate.Date);
            if (existingItinerary != null)
            {
                continue;
            }

            var (style, moment) = _eventOutfitPlanningService.ResolveDayPlan(plannerEvent.Type, i, weather, existingItinerary?.Moment, plannerEvent.PreferredStyles);

            var excludedItemIds = EventReusePolicy.ComputeExcludedItemIds(
                itemUsageByDate, currentDate, plannerEvent.ReuseAfterDays);

            AiGeneratedOutfitDto? outfitResult = null;

            if (useStylist)
            {
                outfitResult = await GenerateOutfitForDayWithStylistAsync(
                    request.UserId, user!, userClothes, style, moment, weather, excludedItemIds, ct);
            }

            if (outfitResult == null)
            {
                outfitResult = await GenerateOutfitForDay(request.UserId, style, moment, weather, excludedItemIds, ct);
            }

            if (outfitResult != null)
            {
                var itemIds = outfitResult.SelectedItems.Select(si => si.Id).ToList();
                var items = await _clothingRepository.GetByIdsAsync(itemIds, ct);

                var outfitName = outfitResult.StylistHeadline != null
                    ? $"{plannerEvent.Name} - Day {i + 1}: {outfitResult.StylistHeadline}"
                    : $"{plannerEvent.Name} - Day {i + 1} ({style})";

                var outfit = Outfit.Create(
                    request.UserId,
                    outfitName,
                    items,
                    _clock.GetUtcNow().UtcDateTime,
                    isAiGenerated: true,
                    isEventExclusive: true);

                await _outfitRepository.AddAsync(outfit, ct);

                var itinerary = EventItinerary.Create(
                    plannerEvent.Id,
                    outfit.Id,
                    currentDate,
                    moment,
                    weather?.Temperature,
                    _clock.GetUtcNow().UtcDateTime);

                await _plannerEventRepository.AddItineraryAsync(itinerary, ct);

                generatedDays.Add(new GeneratedDayDto(
                    currentDate,
                    style,
                    weather?.Condition ?? "Unknown",
                    outfit.Id,
                    outfit.Name
                ));

                itemUsageByDate[currentDate] = items;
            }
        }

        return new GenerateEventOutfitsResult(totalDays, generatedDays.Count, generatedDays, weatherAlert);
    }

    private async Task<AiGeneratedOutfitDto?> GenerateOutfitForDayWithStylistAsync(
        Guid userId,
        User user,
        IReadOnlyList<ClothingItem> allClothes,
        string style,
        string moment,
        WeatherData? weather,
        HashSet<Guid> excludedItemIds,
        CancellationToken ct)
    {
        try
        {
            var available = allClothes.Where(i => !excludedItemIds.Contains(i.Id)).ToList();
            if (available.Count < 3)
            {
                return null;
            }

            var recency = await _clothingRepository.GetWearRecencyAsync(userId, ct);
            var recentlyShown = (await _feedbackRepository.GetRecentlyShownItemIdsAsync(
                userId, _clock.GetUtcNow().UtcDateTime.AddDays(-2), null, ct))?.ToHashSet() ?? new HashSet<Guid>();

            var allowOuterwear = OuterwearPolicy.ShouldIncludeOuterwear(
                user.OuterwearMode, user.OuterwearTempThreshold, weather?.Temperature, temperatureHint: null);

            var pool = await _poolBuilder.BuildAsync(
                new StylistCandidatePoolRequest(
                    userId, moment, style,
                    _occasionFormalityRules.FormalityFor(moment),
                    weather?.Temperature, allowOuterwear,
                    Seed: null, _stylistSettings.MaxCandidates,
                    FavoriteColors: user.FavoriteColors,
                    AvoidColors: user.AvoidColors),
                available, recency, recentlyShown,
                _stylistSettings.MmrLambda, ct);

            var context = new StylistContext(
                moment,
                StylistOutfitComposer.TimeOfDay(_clock.GetLocalNow().DateTime),
                StylistOutfitComposer.DescribeWeather(weather),
                allowOuterwear,
                style,
                FavoriteColors: user.FavoriteColors,
                AvoidColors: user.AvoidColors);

            _logger.LogInformation("Stylist composing event outfit for day (style={Style}, moment={Moment}, pool={Count}).",
                style, moment, pool.Count);

            var result = await _composer.ComposeAsync(userId, pool, context, seed: null, lockSeed: false, shuffle: false, ct);
            if (result == null)
            {
                return null;
            }

            return new AiGeneratedOutfitDto
            {
                GenerationId = Guid.NewGuid(),
                Name = result.Headline ?? $"{style} Look",
                SelectedItems = result.ChosenItems.Select(i => StylistOutfitComposer.ToSimilarItem(i, 1.0)).ToList(),
                RecommendationsPerType = StylistOutfitComposer.BuildStylistRecommendations(result.ChosenItems, result.Pool),
                IsValid = true,
                GeneratedByStylist = true,
                StylistHeadline = result.Headline,
                StylistHighlights = result.Highlights.ToList(),
                StylistTip = result.StylingTip
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Stylist failed for event day (style={Style}); falling back to deterministic.", style);
            return null;
        }
    }

    private async Task<AiGeneratedOutfitDto?> GenerateOutfitForDay(
        Guid userId,
        string style,
        string moment,
        WeatherData? weather,
        HashSet<Guid> excludedItemIds,
        CancellationToken ct)
    {
        var intent = new PromptIntent { Style = style, Occasion = moment };
        var startItem = await _startItemSelector.SelectAsync(userId, intent, excludedItemIds, weather, ct);
        if (startItem == null)
        {
            startItem = await _eventOutfitPlanningService.SelectStartItemAsync(userId, style, weather, excludedItemIds, ct);
        }

        if (startItem?.Embedding == null)
        {
            return null;
        }

        try
        {
            return await _outfitGenerator.GenerateAiOutfitAsync(
                userId,
                startItem.Id,
                new OutfitGenerationOptions
                {
                    Threshold = 0.4,
                    Weather = weather,
                    Style = style,
                    ExcludedItemIds = excludedItemIds
                },
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Outfit generation failed for day with style {Style}; skipping day.", style);
            return null;
        }
    }

}
