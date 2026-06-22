using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Generation;

public sealed record StylistCompositionResult(
    IReadOnlyList<ClothingItem> ChosenItems,
    IReadOnlyList<ClothingItem> Pool,
    string? Headline,
    IReadOnlyList<string> Highlights,
    string? StylingTip);

public sealed class StylistOutfitComposer(
    IOutfitStylist stylist,
    IItemPairScoreRepository pairScoreRepository,
    IUserLearningProfileRepository learningProfileRepository,
    ILogger<StylistOutfitComposer> logger)
{
    public async Task<StylistCompositionResult?> ComposeAsync(
        Guid userId,
        List<ClothingItem> pool,
        StylistContext context,
        ClothingItem? seed,
        bool lockSeed,
        bool shuffle,
        CancellationToken ct)
    {
        if (pool.Count < 3)
        {
            logger.LogWarning("Stylist composition skipped: only {Count} candidates (need 3+).", pool.Count);
            return null;
        }

        var candidateSet = StylistCandidateSet.Build(pool, shuffle);
        int? mandatoryNumber = lockSeed && seed != null ? candidateSet.NumberOf(seed.Id) : null;

        var ctx = context with
        {
            MandatoryItemNumber = mandatoryNumber,
            MandatorySlot = lockSeed ? seed?.Type.ToString().ToLowerInvariant() : context.MandatorySlot,
            Shuffle = shuffle
        };

        var outfits = await stylist.ComposeAsync(candidateSet.Lines, ctx, ct);
        if (outfits is not { Count: > 0 })
        {
            logger.LogWarning("Gemma3 returned no outfits.");
            return null;
        }

        var pairCompatibility = await pairScoreRepository.GetCompatibilityMapAsync(userId, ct)
                                ?? new Dictionary<(Guid, Guid), double>();
        var learningProfile = await learningProfileRepository.GetByUserIdAsync(userId, ct);

        var selection = SelectBestValidOutfit(
            outfits, candidateSet, context.AllowOuterwear, lockSeed, seed,
            pairCompatibility, learningProfile);
        if (selection == null)
        {
            const string validationError =
                "No outfit satisfied the structural contract: exactly one TOP, one BOTTOM and one SHOES item, " +
                "no forbidden OUTERWEAR, and the locked seed when required.";

            var repaired = await stylist.RepairAsync(candidateSet.Lines, ctx, outfits, validationError, ct);
            if (repaired is { Count: > 0 })
            {
                selection = SelectBestValidOutfit(
                    repaired, candidateSet, context.AllowOuterwear, lockSeed, seed,
                    pairCompatibility, learningProfile);
            }
        }

        if (selection == null)
        {
            logger.LogWarning("Gemma3 could not compose a structurally valid outfit.");
            return null;
        }

        var (best, chosen) = selection.Value;
        var grounded = StylistNarrativeGrounder.Ground(best, chosen);

        return new StylistCompositionResult(
            chosen,
            pool,
            string.IsNullOrWhiteSpace(grounded.Headline) ? null : grounded.Headline.Trim(),
            grounded.Highlights.Where(h => !string.IsNullOrWhiteSpace(h)).Select(h => h.Trim()).ToList(),
            string.IsNullOrWhiteSpace(grounded.StylingTip) ? null : grounded.StylingTip.Trim());
    }

    // --- shared static helpers ---

    public static string? DescribeWeather(WeatherData? weather)
    {
        if (weather == null) return null;
        var detail = string.IsNullOrWhiteSpace(weather.ConditionDetail) ? weather.Condition : weather.ConditionDetail;
        var feels = weather.FeelsLike.HasValue ? $" (feels like {weather.FeelsLike.Value:0}C)" : "";
        return $"{weather.Temperature:0}C{feels}, {detail}";
    }

    public static string TimeOfDay(DateTime now)
    {
        var hour = now.Hour;
        return hour switch
        {
            < 11 => "morning",
            < 17 => "afternoon",
            < 21 => "evening",
            _ => "night"
        };
    }

    public static SimilarItemDto ToSimilarItem(ClothingItem item, double score) => new()
    {
        Id = item.Id,
        Name = item.Name,
        ProcessedImageUrl = item.ProcessedImageUrl,
        SimilarityScore = score
    };

    public static IReadOnlyList<OutfitRecommendationDto> BuildStylistRecommendations(
        IReadOnlyList<ClothingItem> chosen,
        IReadOnlyList<ClothingItem> pool)
    {
        var selectedIds = chosen.Select(i => i.Id).ToHashSet();
        var recommendations = new List<OutfitRecommendationDto>();

        foreach (var type in chosen.Select(i => i.Type).Distinct().OrderBy(TypeOrder))
        {
            var selectedForType = chosen.Where(i => i.Type == type).ToList();
            var alternatives = pool
                .Where(i => i.Type == type && !selectedIds.Contains(i.Id))
                .DistinctBy(i => i.Id)
                .Take(Math.Max(0, 5 - selectedForType.Count))
                .ToList();

            var candidates = new List<SimilarItemDto>();
            candidates.AddRange(selectedForType.Select(i => ToSimilarItem(i, 1.0)));
            for (var i = 0; i < alternatives.Count; i++)
            {
                candidates.Add(ToSimilarItem(alternatives[i], Math.Max(0.25, 0.82 - i * 0.08)));
            }

            recommendations.Add(new OutfitRecommendationDto
            {
                Type = type,
                TopCandidates = candidates
            });
        }

        return recommendations;
    }

    private static int TypeOrder(ClothingType type) => type switch
    {
        ClothingType.Top => 0,
        ClothingType.Bottom => 1,
        ClothingType.Shoes => 2,
        ClothingType.Outerwear => 3,
        ClothingType.Accessory => 4,
        _ => 99
    };

    private static (StylistOutfit Outfit, List<ClothingItem> Items)? SelectBestValidOutfit(
        IReadOnlyList<StylistOutfit> outfits,
        StylistCandidateSet candidateSet,
        bool allowOuterwear,
        bool lockSeed,
        ClothingItem? seed,
        IReadOnlyDictionary<(Guid, Guid), double> pairCompatibility,
        UserLearningProfile? learningProfile)
    {
        var valid = new List<(StylistOutfit Outfit, List<ClothingItem> Items, double Score, int Index)>();
        for (var index = 0; index < outfits.Count; index++)
        {
            var outfit = outfits[index];
            var resolved = candidateSet.Resolve(outfit.ItemNumbers);
            if (lockSeed && seed != null && resolved.All(c => c.Id != seed.Id))
            {
                resolved = resolved.Where(c => c.Type != seed.Type).Prepend(seed).ToList();
            }

            var chosen = BodySlotDeduplicator.Deduplicate(resolved);
            if (IsStructurallyValid(chosen, allowOuterwear, lockSeed, seed))
            {
                valid.Add((outfit, chosen, PreferenceScore(chosen, pairCompatibility, learningProfile), index));
            }
        }

        var best = valid
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Index)
            .FirstOrDefault();

        return best.Outfit == null ? null : (best.Outfit, best.Items);
    }

    private static double PreferenceScore(
        IReadOnlyList<ClothingItem> items,
        IReadOnlyDictionary<(Guid, Guid), double> pairCompatibility,
        UserLearningProfile? learningProfile)
    {
        double score = 0;
        var knownPairs = new List<double>();
        for (var i = 0; i < items.Count; i++)
        {
            for (var j = i + 1; j < items.Count; j++)
            {
                if (pairCompatibility.TryGetValue(ItemPair.Canonical(items[i].Id, items[j].Id), out var compatibility))
                {
                    knownPairs.Add(compatibility);
                }
            }
        }

        if (knownPairs.Count > 0)
        {
            score += knownPairs.Average() * 0.75;
            score -= knownPairs.Count(value => value <= -0.5) * 2.0;
        }

        if (learningProfile != null)
        {
            var learnedScores = new List<double>();
            foreach (var item in items)
            {
                var colorKey = TasteKey.Color(item.Color);
                if (colorKey != null && learningProfile.ColorScores.TryGetValue(colorKey, out var colorScore))
                    learnedScores.Add(colorScore);

                var styleKey = TasteKey.Style(item.Usage);
                if (styleKey != null && learningProfile.StyleScores.TryGetValue(styleKey, out var styleScore))
                    learnedScores.Add(styleScore);
            }

            if (learnedScores.Count > 0)
                score += ((learnedScores.Average() - 0.5) * 2.0) * 0.30;
        }

        score += items.Count(item => item.IsFavorite) * 0.03;
        return score;
    }

    private static bool IsStructurallyValid(
        IReadOnlyList<ClothingItem> chosen,
        bool allowOuterwear,
        bool lockSeed,
        ClothingItem? seed)
    {
        if (lockSeed && seed != null && chosen.All(c => c.Id != seed.Id)) return false;
        if (!allowOuterwear && chosen.Any(c => c.Type == ClothingType.Outerwear)) return false;

        return chosen.Any(c => c.Type == ClothingType.Top)
               && chosen.Any(c => c.Type == ClothingType.Bottom)
               && chosen.Any(c => c.Type == ClothingType.Shoes);
    }
}
