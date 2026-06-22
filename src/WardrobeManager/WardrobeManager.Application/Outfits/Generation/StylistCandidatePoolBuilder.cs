using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Feasibility;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Generation;

public sealed record StylistCandidatePoolRequest(
    Guid UserId,
    string? Occasion,
    string? Style,
    int? TargetFormality,
    double? Temperature,
    bool AllowOuterwear,
    ClothingItem? Seed,
    int MaxCandidates,
    IReadOnlyList<string>? FavoriteColors = null,
    IReadOnlyList<string>? AvoidColors = null);

// build the fashionclip candidate slate without deciding final compatibility.
public sealed class StylistCandidatePoolBuilder(
    IClothingRepository clothingRepository,
    IMlService mlService,
    IThermalRules thermal,
    ILogger<StylistCandidatePoolBuilder> logger)
{
    private const int SearchMultiplier = 4;

    private static readonly (ClothingType Type, int Cap)[] SlotCaps =
    {
        (ClothingType.Top, 7),
        (ClothingType.Bottom, 7),
        (ClothingType.Shoes, 5),
        (ClothingType.Outerwear, 3),
        (ClothingType.Accessory, 2),
    };

    public async Task<List<ClothingItem>> BuildAsync(
        StylistCandidatePoolRequest request,
        IReadOnlyList<ClothingItem> wardrobe,
        IReadOnlyDictionary<Guid, DateTime> recency,
        IReadOnlySet<Guid> recentlyShown,
        double mmrLambda,
        CancellationToken ct = default)
    {
        var selected = new List<ClothingItem>();
        var used = new HashSet<Guid>();
        int max = request.MaxCandidates <= 0 ? SlotCaps.Sum(s => s.Cap) : request.MaxCandidates;
        int? targetRank = request.TargetFormality.HasValue
            ? FormalityScale.RankOfFormalityLevel(request.TargetFormality.Value)
            : null;
        var weather = request.Temperature.HasValue
            ? new WeatherData((float)request.Temperature.Value, "", "")
            : null;

        foreach (var (type, cap) in SlotCaps)
        {
            if (selected.Count >= max) break;
            if (type == ClothingType.Outerwear && !request.AllowOuterwear) continue;

            var remaining = max - selected.Count;
            var slotCap = Math.Min(cap, remaining);
            if (slotCap <= 0) break;

            var candidates = await BuildSlotCandidatesAsync(request, wardrobe, type, slotCap, ct);
            var scored = candidates
                .Where(c => !used.Contains(c.Item.Id))
                .Where(c => !MatchesAnyColor(c.Item.Color, request.AvoidColors))
                .Select(c => (
                    c.Item,
                    Relevance: ScoreCandidate(c.Item, c.ClipSimilarity, targetRank, weather, recency,
                        request.Seed?.Embedding, recentlyShown, request.FavoriteColors),
                    c.Item.Embedding))
                .Where(c => c.Relevance > 0)
                .ToList();

            var picks = MmrSelector.Select(scored, slotCap, mmrLambda);
            foreach (var item in picks)
            {
                if (used.Add(item.Id)) selected.Add(item);
            }
        }

        return selected;
    }

    private async Task<List<PoolCandidate>> BuildSlotCandidatesAsync(
        StylistCandidatePoolRequest request,
        IReadOnlyList<ClothingItem> wardrobe,
        ClothingType type,
        int slotCap,
        CancellationToken ct)
    {
        var byId = new Dictionary<Guid, PoolCandidate>();
        var query = BuildSlotQuery(type, request);

        try
        {
            var vector = await mlService.EmbedTextAsync(query, ct);
            if (vector.Length > 0)
            {
                var retrieved = await clothingRepository.GetSimilarItemsAsync(
                    request.UserId,
                    vector,
                    type,
                    limit: Math.Max(slotCap * SearchMultiplier, slotCap),
                    threshold: null,
                    gender: NormalizeGender(request.Seed?.Gender),
                    ct: ct);

                foreach (var (item, similarity) in retrieved)
                {
                    byId[item.Id] = new PoolCandidate(item, similarity);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "FashionCLIP slot retrieval failed for {Slot}; falling back to wardrobe scan.", type);
        }

        // keep each slot populated when text embedding is weak.
        foreach (var item in wardrobe.Where(i => i.Type == type))
        {
            byId.TryAdd(item.Id, new PoolCandidate(item, null));
        }

        return byId.Values.ToList();
    }

    private double ScoreCandidate(
        ClothingItem item,
        double? clipSimilarity,
        int? targetRank,
        WeatherData? weather,
        IReadOnlyDictionary<Guid, DateTime> recency,
        float[]? seedEmbedding,
        IReadOnlySet<Guid> recentlyShown,
        IReadOnlyList<string>? favoriteColors)
    {
        if (weather != null && thermal.IsWeatherVetoed(item, weather)) return 0;

        double score = 1.0;

        if (clipSimilarity.HasValue)
        {
            var normalizedClip = Math.Clamp((clipSimilarity.Value + 1.0) / 2.0, 0.0, 1.0);
            score *= 0.65 + 0.70 * normalizedClip;
        }
        else
        {
            score *= 0.75;
        }

        if (targetRank is int tr)
        {
            int diff = Math.Abs(FormalityScale.RankOf(item) - tr);
            score *= diff switch { 0 => 1.0, 1 => 0.82, 2 => 0.45, _ => 0.20 };
        }

        if (seedEmbedding != null && item.Embedding != null)
        {
            score *= 1.0 + 0.25 * Math.Max(0, VectorSimilarity.Cosine(seedEmbedding, item.Embedding));
        }

        if (recency.TryGetValue(item.Id, out var last))
        {
            var days = (DateTime.UtcNow - last).TotalDays;
            score *= days < 2 ? 0.60 : days < 7 ? 0.85 : 1.0;
        }
        else
        {
            score *= 1.08;
        }

        if (recentlyShown.Contains(item.Id)) score *= 0.50;
        if (item.IsFavorite) score *= 1.08;
        if (MatchesAnyColor(item.Color, favoriteColors)) score *= 1.18;

        return score;
    }

    private static bool MatchesAnyColor(string? itemColor, IReadOnlyList<string>? colors) =>
        colors is { Count: > 0 } && colors.Any(color => ColorFamily.ColorsMatch(itemColor, color));

    private static string BuildSlotQuery(ClothingType type, StylistCandidatePoolRequest request)
    {
        var slot = type switch
        {
            ClothingType.Top => "top shirt blouse knit tee",
            ClothingType.Bottom => "bottom pants trousers jeans skirt",
            ClothingType.Shoes => "shoes footwear",
            ClothingType.Outerwear => "jacket coat blazer outerwear layer",
            ClothingType.Accessory => "accessory bag belt scarf",
            _ => "clothing item"
        };

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Style)) parts.Add(request.Style!);
        if (!string.IsNullOrWhiteSpace(request.Occasion)) parts.Add(request.Occasion!);
        if (request.TargetFormality is int f) parts.Add(FormalityPhrase(f));
        if (request.Temperature is double t)
        {
            if (t <= 10) parts.Add("cold weather");
            else if (t >= 25) parts.Add("warm weather lightweight");
        }
        parts.Add(slot);

        return $"a {string.Join(' ', parts)} clothing item";
    }

    private static string FormalityPhrase(int formality) => formality switch
    {
        <= 1 => "very casual",
        2 => "casual",
        3 => "smart casual",
        4 => "business casual",
        _ => "formal elegant"
    };

    private static string? NormalizeGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender)) return null;
        return gender.Equals("Unisex", StringComparison.OrdinalIgnoreCase) ? null : gender;
    }

    private sealed record PoolCandidate(ClothingItem Item, double? ClipSimilarity);
}
