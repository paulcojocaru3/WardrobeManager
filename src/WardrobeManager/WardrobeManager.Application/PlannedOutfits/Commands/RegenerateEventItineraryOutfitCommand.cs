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
    private readonly IUserRepository _userRepository;
    private readonly StylistOutfitComposer _composer;
    private readonly StylistCandidatePoolBuilder _poolBuilder;
    private readonly StylistSettings _stylistSettings;
    private readonly IOccasionFormalityRules _occasionFormalityRules;
    private readonly IOutfitFeedbackRepository _feedbackRepository;
    private readonly ILogger<RegenerateEventItineraryOutfitCommandHandler> _logger;
    private readonly TimeProvider _clock;

    public RegenerateEventItineraryOutfitCommandHandler(
        IPlannerEventRepository plannerEventRepository,
        IOutfitRepository outfitRepository,
        IOutfitGenerator outfitGenerator,
        IClothingRepository clothingRepository,
        IEventOutfitPlanningService eventOutfitPlanningService,
        IWeatherService weatherService,
        IUserRepository userRepository,
        StylistOutfitComposer composer,
        StylistCandidatePoolBuilder poolBuilder,
        StylistSettings stylistSettings,
        IOccasionFormalityRules occasionFormalityRules,
        IOutfitFeedbackRepository feedbackRepository,
        ILogger<RegenerateEventItineraryOutfitCommandHandler> logger,
        TimeProvider? clock = null)
    {
        _plannerEventRepository = plannerEventRepository;
        _outfitRepository = outfitRepository;
        _outfitGenerator = outfitGenerator;
        _clothingRepository = clothingRepository;
        _eventOutfitPlanningService = eventOutfitPlanningService;
        _weatherService = weatherService;
        _userRepository = userRepository;
        _composer = composer;
        _poolBuilder = poolBuilder;
        _stylistSettings = stylistSettings;
        _occasionFormalityRules = occasionFormalityRules;
        _feedbackRepository = feedbackRepository;
        _logger = logger;
        _clock = clock ?? TimeProvider.System;
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

        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        var useStylist = user?.UseGemmaStylistForOutfits == true && _stylistSettings.Enabled;
        var usageByDate = EventReusePolicy.BuildUsageMap(plannerEvent.Itineraries);
        var excludedItemIds = EventReusePolicy.ComputeExcludedItemIds(
            usageByDate, itinerary.Date, plannerEvent.ReuseAfterDays);
        if (itinerary.Outfit != null)
        {
            excludedItemIds.UnionWith(itinerary.Outfit.Items.Select(item => item.Id));
        }

        AiGeneratedOutfitDto? aiResult = null;

        if (useStylist)
        {
            try
            {
                var allClothes = await _clothingRepository.GetByUserIdAsync(request.UserId, ct);
                var available = allClothes.Where(i => !excludedItemIds.Contains(i.Id)).ToList();

                if (available.Count >= 3)
                {
                    var recency = await _clothingRepository.GetWearRecencyAsync(request.UserId, ct);
                    var recentlyShown = (await _feedbackRepository.GetRecentlyShownItemIdsAsync(
                        request.UserId, _clock.GetUtcNow().UtcDateTime.AddDays(-2), null, ct))?.ToHashSet() ?? new HashSet<Guid>();

                    var allowOuterwear = OuterwearPolicy.ShouldIncludeOuterwear(
                        user!.OuterwearMode, user.OuterwearTempThreshold, weather?.Temperature, temperatureHint: null);

                    var pool = await _poolBuilder.BuildAsync(
                        new StylistCandidatePoolRequest(
                            request.UserId, itinerary.Moment, style,
                            _occasionFormalityRules.FormalityFor(itinerary.Moment),
                            weather?.Temperature, allowOuterwear,
                            Seed: null, _stylistSettings.MaxCandidates,
                            FavoriteColors: user.FavoriteColors,
                            AvoidColors: user.AvoidColors),
                        available, recency, recentlyShown,
                        _stylistSettings.MmrLambda, ct);

                    var context = new StylistContext(
                        itinerary.Moment,
                        StylistOutfitComposer.TimeOfDay(_clock.GetLocalNow().DateTime),
                        StylistOutfitComposer.DescribeWeather(weather),
                        allowOuterwear,
                        style,
                        FavoriteColors: user.FavoriteColors,
                        AvoidColors: user.AvoidColors);

                    var composed = await _composer.ComposeAsync(request.UserId, pool, context, seed: null, lockSeed: false, shuffle: false, ct);
                    if (composed != null)
                    {
                        aiResult = new AiGeneratedOutfitDto
                        {
                            GenerationId = Guid.NewGuid(),
                            Name = composed.Headline ?? $"{style} Look",
                            SelectedItems = composed.ChosenItems.Select(i => StylistOutfitComposer.ToSimilarItem(i, 1.0)).ToList(),
                            IsValid = true,
                            GeneratedByStylist = true,
                            StylistHeadline = composed.Headline,
                            StylistHighlights = composed.Highlights.ToList(),
                            StylistTip = composed.StylingTip
                        };
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Stylist failed for regeneration; falling back to deterministic.");
            }
        }

        if (aiResult == null)
        {
            var startItem = await _eventOutfitPlanningService.SelectStartItemAsync(request.UserId, style, weather, excludedItemIds, ct);
            if (startItem == null || startItem.Embedding == null)
            {
                return false;
            }

            aiResult = await _outfitGenerator.GenerateAiOutfitAsync(
                request.UserId,
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

        var itemIds = aiResult.SelectedItems.Select(si => si.Id).ToList();
        var items = await _clothingRepository.GetByIdsAsync(itemIds, ct);

        if (items.Count == 0)
        {
            return false;
        }

        var outfitName = aiResult.StylistHeadline != null
            ? $"{plannerEvent.Name} - {itinerary.Date:yyyy-MM-dd}: {aiResult.StylistHeadline}"
            : $"{plannerEvent.Name} - {itinerary.Date:yyyy-MM-dd} ({style})";

        var newOutfit = Outfit.Create(
            request.UserId,
            outfitName,
            items,
            _clock.GetUtcNow().UtcDateTime,
            isAiGenerated: true,
            isEventExclusive: true);

        await _outfitRepository.AddAsync(newOutfit, ct);

        itinerary.AssignOutfit(newOutfit.Id, weather?.Temperature);
        await _plannerEventRepository.UpdateItineraryAsync(itinerary, ct);

        return true;
    }
}
