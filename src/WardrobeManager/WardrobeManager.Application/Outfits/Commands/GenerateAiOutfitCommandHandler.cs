using MediatR;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Commands;

public sealed class GenerateAiOutfitCommandHandler(
    IOutfitGenerator outfitGenerator,
    IWeatherService weatherService,
    IClothingRepository clothingRepository,
    IUserRepository userRepository,
    IOutfitFeedbackRepository feedbackRepository,
    IOccasionFormalityRules occasionFormalityRules,
    StylistOutfitComposer composer,
    StylistCandidatePoolBuilder poolBuilder,
    StylistSettings stylistSettings,
    ILogger<GenerateAiOutfitCommandHandler> logger,
    TimeProvider? clock = null) : IRequestHandler<GenerateAiOutfitCommand, AiGeneratedOutfitDto>
{
    public async Task<AiGeneratedOutfitDto> Handle(GenerateAiOutfitCommand request, CancellationToken ct)
    {
        WeatherData? weather = null;
        if (!string.IsNullOrEmpty(request.City))
        {
            weather = await weatherService.GetCurrentWeatherAsync(request.City, ct);
        }

        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        var startItemId = await ResolveStartItemAsync(request, user?.AvoidColors ?? [], ct);

        if (user?.UseGemmaStylistForOutfits == true)
        {
            if (!stylistSettings.Enabled)
            {
                throw new InvalidOperationException("Gemma3 outfit styling is disabled on the server.");
            }

            return await GenerateWithStylistOnlyAsync(request, weather, user, startItemId, ct);
        }

        var dto = await outfitGenerator.GenerateAiOutfitAsync(
            request.UserId,
            startItemId,
            new OutfitGenerationOptions
            {
                Threshold = request.Threshold,
                Weather = weather,
                Style = request.Style,
                Occasion = request.Occasion,
                Formality = occasionFormalityRules.FormalityFor(request.Occasion),
                PreferUnusedItems = request.PreferUnusedItems || request.AnchorOnUnused
            },
            ct);

        return dto;
    }

    private async Task<AiGeneratedOutfitDto> GenerateWithStylistOnlyAsync(
        GenerateAiOutfitCommand request,
        WeatherData? weather,
        User user,
        Guid startItemId,
        CancellationToken ct)
    {
        var all = await clothingRepository.GetByUserIdAsync(request.UserId, ct);
        if (all.Count == 0)
        {
            throw new InvalidOperationException("Your wardrobe needs at least a top, bottoms and shoes before Gemma3 can style an outfit.");
        }

        var recency = await clothingRepository.GetWearRecencyAsync(request.UserId, ct);
        var recentlyShown = (await feedbackRepository.GetRecentlyShownItemIdsAsync(
            request.UserId, (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime.AddDays(-2), null, ct))?.ToHashSet() ?? new HashSet<Guid>();

        var seed = all.FirstOrDefault(i => i.Id == startItemId);
        var allowOuterwear = OuterwearPolicy.ShouldIncludeOuterwear(
            user.OuterwearMode, user.OuterwearTempThreshold, weather?.Temperature, temperatureHint: null);

        var lockSeed = !request.AnchorOnUnused
            && request.StartItemId is { } picked
            && picked != Guid.Empty
            && seed != null;
        var occasionForStylist = request.Occasion ?? request.Style;

        var pool = await poolBuilder.BuildAsync(
            new StylistCandidatePoolRequest(
                request.UserId,
                occasionForStylist,
                request.Style,
                occasionFormalityRules.FormalityFor(occasionForStylist),
                weather?.Temperature,
                allowOuterwear,
                seed,
                stylistSettings.MaxCandidates,
                FavoriteColors: user.FavoriteColors,
                AvoidColors: user.AvoidColors),
            all,
            recency,
            recentlyShown,
            stylistSettings.MmrLambda,
            ct);

        if (lockSeed && seed != null && pool.All(i => i.Id != seed.Id))
        {
            pool.Add(seed);
        }

        var context = new StylistContext(
            occasionForStylist,
            StylistOutfitComposer.TimeOfDay((clock ?? TimeProvider.System).GetLocalNow().DateTime),
            StylistOutfitComposer.DescribeWeather(weather),
            allowOuterwear,
            request.Style,
            FavoriteColors: user.FavoriteColors,
            AvoidColors: user.AvoidColors);

        logger.LogInformation("Generating outfit synchronously with gemma3 stylist for user {UserId} from {Count} candidates.",
            request.UserId, pool.Count);

        var result = await composer.ComposeAsync(request.UserId, pool, context, seed, lockSeed, request.Shuffle, ct);
        if (result == null)
        {
            throw new InvalidOperationException("Gemma3 could not compose a structurally valid outfit. Try again or turn off Gemma3-only generation.");
        }

        var name = result.Headline != null
            ? result.Headline
            : $"{(request.Style ?? occasionForStylist ?? "Stylist")} Look";

        return new AiGeneratedOutfitDto
        {
            GenerationId = Guid.NewGuid(),
            Name = name,
            SelectedItems = result.ChosenItems.Select(i => StylistOutfitComposer.ToSimilarItem(i, 1.0)).ToList(),
            RecommendationsPerType = StylistOutfitComposer.BuildStylistRecommendations(result.ChosenItems, result.Pool),
            IsValid = true,
            Warnings = Array.Empty<string>(),
            Candidates = Array.Empty<OutfitCandidate>(),
            GeneratedByStylist = true,
            StylistHeadline = result.Headline,
            StylistHighlights = result.Highlights.ToList(),
            StylistTip = result.StylingTip
        };
    }

    private async Task<Guid> ResolveStartItemAsync(
        GenerateAiOutfitCommand request,
        IReadOnlyList<string> avoidColors,
        CancellationToken ct)
    {
        if (!request.AnchorOnUnused && request.StartItemId is { } explicitId && explicitId != Guid.Empty)
        {
            return explicitId;
        }

        // seed rediscover on a rarely worn item without repeating recent seeds.
        var candidates = await clothingRepository.GetLeastWornCandidatesAsync(request.UserId, request.Style, limit: 30, ct);
        candidates = candidates
            .Where(item => !avoidColors.Any(color => ColorFamily.ColorsMatch(item.Color, color)))
            .ToList();
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("No wardrobe items outside your avoided colors are available to build an outfit.");
        }

        var recentlySeeded = await feedbackRepository.GetRecentlyShownItemIdsAsync(
            request.UserId, (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime.AddDays(-3), ct: ct);
        var recentSet = recentlySeeded as ISet<Guid> ?? new HashSet<Guid>(recentlySeeded);

        var pool = candidates.Where(c => !recentSet.Contains(c.Id)).ToList();
        if (pool.Count == 0) pool = candidates;

        return WeightedPick(pool).Id;
    }

    private static ClothingItem WeightedPick(IReadOnlyList<ClothingItem> sortedByWear)
    {
        int n = sortedByWear.Count;
        if (n == 1) return sortedByWear[0];

        double totalWeight = (double)n * (n + 1) / 2.0;
        double roll = Random.Shared.NextDouble() * totalWeight;

        double cumulative = 0;
        for (int i = 0; i < n; i++)
        {
            cumulative += n - i;
            if (roll <= cumulative) return sortedByWear[i];
        }
        return sortedByWear[n - 1];
    }
}
