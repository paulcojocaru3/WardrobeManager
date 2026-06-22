using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Feasibility;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Prompting;

public sealed class StartItemSelector(
    IClothingRepository clothingRepository,
    IMlService mlService,
    IGarmentFeasibility feasibility,
    ILogger<StartItemSelector> logger) : IStartItemSelector
{
    public async Task<ClothingItem?> SelectAsync(Guid userId, PromptIntent intent, IReadOnlyCollection<Guid>? excludedItemIds = null, WeatherData? weather = null, CancellationToken ct = default)
    {
        // a specific garment named in the prompt anchors the seed; its sub-type hard-filters the pool.
        RequestedGarment? seedGarment = null;
        if (intent.RequestedGarments.Count > 0)
        {
            seedGarment = intent.RequestedGarments[0];
        }

        // generic words ("top"/"pants"/"shoes") fix only the type, not the sub-type — don't hard-filter on them.
        string? seedSubType = null;
        if (seedGarment != null && !GarmentVocabulary.IsGenericSubType(seedGarment.SubType))
            seedSubType = seedGarment.SubType;

        ClothingType? preferredType = seedGarment?.Type;
        if (preferredType == null && intent.RequestedTypes.Count > 0)
        {
            preferredType = intent.RequestedTypes[0];
        }

        // colors that constrain the SEED's slot specifically (e.g. "black pants" -> the bottom seed
        var (seedDesired, seedAvoid) = ResolveSeedColors(intent, preferredType);

        string? queryText = BuildQueryText(intent, seedDesired);
        if (!string.IsNullOrWhiteSpace(queryText))
        {
            float[] vector;
            try { vector = await mlService.EmbedTextAsync(queryText, ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Text embedding failed; falling back to attribute filtering.");
                vector = Array.Empty<float>();
            }

            if (vector.Length > 0)
            {
                // visual ranking: top candidates by cosine similarity within the requested type.
                var pool = await clothingRepository.GetSimilarItemsAsync(
                    userId, vector, preferredType, limit: 20, threshold: null, ct: ct);

                // if nothing in that type, retry across all types.
                if (pool.Count == 0 && preferredType.HasValue)
                    pool = await clothingRepository.GetSimilarItemsAsync(
                        userId, vector, null, limit: 20, threshold: null, ct: ct);

                pool = Exclude(pool, excludedItemIds);

                if (seedSubType != null)
                {
                    var bySubType = pool
                        .Where(p => string.Equals(p.Item.SubType, seedSubType, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (bySubType.Count > 0) pool = bySubType;
                }

                var seedContext = BuildSeedContext(intent, seedAvoid, weather);
                var allowed = pool.Where(p => feasibility.Check(p.Item, seedContext).IsFeasible).ToList();
                if (allowed.Count > 0) pool = allowed;

                // on regeneration ("Generate another"), push seeds most visually different from the
                if (excludedItemIds is { Count: > 0 } && pool.Count > 0)
                    pool = await DiversifyAgainstAsync(pool, excludedItemIds, ct);

                var best = PickBest(pool, intent.Style, seedDesired);
                if (best != null)
                {
                    logger.LogDebug("Seed via embedding path: {SeedId} for prompt query '{Query}'.", best.Id, queryText);
                    return best;
                }
            }
            else
            {
                logger.LogWarning("Text embedding empty for '{Query}'; using attribute fallback (non-semantic).", queryText);
            }
        }

        return await FallbackSelectAsync(userId, intent, preferredType, seedSubType, seedDesired, seedAvoid, excludedItemIds, weather, ct);
    }

    // per-slot colors for the seed's clothing type, or the outfit-level colors when the seed type
    private static (IReadOnlyList<string> Desired, IReadOnlyList<string> Avoid) ResolveSeedColors(
        PromptIntent intent, ClothingType? seedType)
    {
        if (seedType.HasValue)
        {
            var spec = intent.GarmentSpecs.FirstOrDefault(s => s.Type == seedType.Value);
            if (spec != null && (spec.DesiredColors.Count > 0 || spec.AvoidColors.Count > 0))
                return (spec.DesiredColors, spec.AvoidColors);
        }
        return (intent.DesiredColors, intent.AvoidColors);
    }

    private static List<(ClothingItem Item, double Similarity)> Exclude(
        List<(ClothingItem Item, double Similarity)> pool, IReadOnlyCollection<Guid>? excluded)
        => excluded is { Count: > 0 } ? pool.Where(p => !excluded.Contains(p.Item.Id)).ToList() : pool;

    // re-rank so items most dissimilar to the already-shown seeds come first (diversity on
    private async Task<List<(ClothingItem Item, double Similarity)>> DiversifyAgainstAsync(
        List<(ClothingItem Item, double Similarity)> pool, IReadOnlyCollection<Guid> excludedIds, CancellationToken ct)
    {
        var shown = await clothingRepository.GetByIdsAsync(excludedIds, ct);
        var shownVectors = shown.Where(i => i.Embedding != null).Select(i => i.Embedding!).ToList();
        if (shownVectors.Count == 0) return pool;

        return pool
            .OrderBy(p => p.Item.Embedding == null ? 1.0 : MaxCosine(p.Item.Embedding, shownVectors))
            .ToList();
    }

    private static double MaxCosine(float[] vector, List<float[]> others)
    {
        double max = -1.0;
        foreach (var other in others)
        {
            var c = Cosine(vector, other);
            if (c > max) max = c;
        }
        return max;
    }

    private static double Cosine(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;
        int n = Math.Min(a.Length, b.Length);
        for (int i = 0; i < n; i++)
        {
            dot += a[i] * (double)b[i];
            na += a[i] * (double)a[i];
            nb += b[i] * (double)b[i];
        }
        const double epsilon = 1e-12;
        if (na < epsilon || nb < epsilon) return 0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    // default seed-style preference when the prompt names no style: lower = picked first.
    private static int StylePriority(string? usage)
    {
        if (string.IsNullOrWhiteSpace(usage)) return 100;
        bool Has(string s) => usage.Contains(s, StringComparison.OrdinalIgnoreCase);
        if (Has("Smart Casual")) return 1; // check before "Casual" (substring)
        if (Has("Casual")) return 0;
        if (Has("Sports")) return 2;
        if (Has("Travel")) return 3;
        if (Has("Party")) return 4;
        if (Has("Ethnic")) return 5;
        if (Has("Formal")) return 6;
        return 100;
    }

    private static ClothingItem? PickBest(IReadOnlyList<(ClothingItem Item, double Similarity)> pool, string? style, IReadOnlyList<string> desiredColors)
    {
        if (pool.Count == 0) return null;

        bool hasStyle = !string.IsNullOrWhiteSpace(style);
        bool hasColors = desiredColors.Count > 0;

        bool MatchesStyle((ClothingItem Item, double Similarity) p) =>
            p.Item.Usage != null && p.Item.Usage.Contains(style!, StringComparison.OrdinalIgnoreCase);
        bool MatchesColor((ClothingItem Item, double Similarity) p) =>
            p.Item.Color != null && desiredColors.Any(dc => p.Item.Color.Contains(dc, StringComparison.OrdinalIgnoreCase));

        // a piece is "mono-color" for this request if it carries exactly one of the asked colors.
        int DesiredColorCount((ClothingItem Item, double Similarity) p) =>
            p.Item.Color == null ? 0 : desiredColors.Count(dc => p.Item.Color.Contains(dc, StringComparison.OrdinalIgnoreCase));
        bool MatchesOneColor((ClothingItem Item, double Similarity) p) => DesiredColorCount(p) == 1;

        bool multipleColors = desiredColors.Count > 1;

        // 1. Best: matches the requested style AND a requested color.
        if (hasStyle && hasColors)
        {
            // with several requested colors, prefer a single-color piece so the palette spreads across the outfit.
            if (multipleColors)
            {
                var styledMono = pool.FirstOrDefault(p => MatchesStyle(p) && MatchesOneColor(p));
                if (styledMono.Item != null) return styledMono.Item;
            }

            var both = pool.FirstOrDefault(p => MatchesStyle(p) && MatchesColor(p));
            if (both.Item != null) return both.Item;
        }

        if (hasStyle)
        {
            var styled = pool.FirstOrDefault(MatchesStyle);
            if (styled.Item != null) return styled.Item;
        }

        // 2b. No style asked -> default preference: casual first ... formal last (honoring colors,
        if (!hasStyle)
        {
            var pick = pool
                .Where(p => !hasColors || MatchesColor(p))
                .OrderBy(p => StylePriority(p.Item.Usage))
                .ThenBy(p => multipleColors && !MatchesOneColor(p) ? 1 : 0)
                .Select(p => p.Item)
                .FirstOrDefault();
            if (pick != null) return pick;
        }

        // 3. Otherwise a requested color (still preferring a single-color piece).
        if (hasColors)
        {
            if (multipleColors)
            {
                var mono = pool.FirstOrDefault(MatchesOneColor);
                if (mono.Item != null) return mono.Item;
            }

            var colored = pool.FirstOrDefault(MatchesColor);
            if (colored.Item != null) return colored.Item;
        }

        // 4. Fallback: most visually similar (pool is ordered best-first).
        return pool[0].Item;
    }

    private static string? BuildQueryText(PromptIntent intent, IReadOnlyList<string> desiredColors)
    {
        // soft attributes (sleeve/material/fit) the LLM may have left out of the phrase.
        var attributes = intent.Attributes;

        // an explicitly described garment is the strongest anchor.
        if (!string.IsNullOrWhiteSpace(intent.AnchorDescription))
        {
            var anchor = intent.AnchorDescription!.Trim();
            // fold in any requested colors / attributes the model left out of the phrase.
            var prefix = desiredColors.Concat(attributes)
                .Where(t => !anchor.Contains(t, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return prefix.Count > 0 ? $"{string.Join(" ", prefix)} {anchor}" : anchor;
        }

        var parts = new List<string>();
        // anchor the seed on the primary color only; concatenating several colors (e.g. "white black")
        if (desiredColors.Count > 0) parts.Add(desiredColors[0]);
        parts.AddRange(attributes);
        if (!string.IsNullOrWhiteSpace(intent.Style)) parts.Add(intent.Style!);
        if (!string.IsNullOrWhiteSpace(intent.Occasion)) parts.Add(intent.Occasion!);

        var text = string.Join(" ", parts).Trim();
        return string.IsNullOrWhiteSpace(text) ? null : $"a {text} clothing item";
    }

    private async Task<ClothingItem?> FallbackSelectAsync(
        Guid userId, PromptIntent intent, ClothingType? preferredType, string? seedSubType,
        IReadOnlyList<string> seedDesired, IReadOnlyList<string> seedAvoid,
        IReadOnlyCollection<Guid>? excludedItemIds, WeatherData? weather, CancellationToken ct)
    {
        var all = await clothingRepository.GetByUserIdAsync(userId, ct);
        if (all.Count == 0) return null;

        var available = excludedItemIds is { Count: > 0 }
            ? all.Where(c => !excludedItemIds.Contains(c.Id)).ToList()
            : all;
        if (available.Count == 0) available = all;

        IEnumerable<ClothingItem> pool = available;

        // same hard-veto filter as the embedding path, via the shared feasibility module.
        var seedContext = BuildSeedContext(intent, seedAvoid, weather);
        var allowed = pool.Where(c => feasibility.Check(c, seedContext).IsFeasible).ToList();
        if (allowed.Count > 0) pool = allowed;

        // prefer the exact requested sub-type when the wardrobe has it (graceful otherwise).
        if (!string.IsNullOrWhiteSpace(seedSubType))
        {
            var bySubType = pool.Where(c => string.Equals(c.SubType, seedSubType, StringComparison.OrdinalIgnoreCase)).ToList();
            if (bySubType.Count > 0) pool = bySubType;
        }

        if (!string.IsNullOrWhiteSpace(intent.Style))
            pool = pool.Where(c => c.Usage != null && c.Usage.Contains(intent.Style!, StringComparison.OrdinalIgnoreCase));

        if (seedDesired.Count > 0)
        {
            var colorPool = pool.Where(c => c.Color != null && seedDesired.Any(dc => c.Color.Contains(dc, StringComparison.OrdinalIgnoreCase))).ToList();
            if (colorPool.Count > 0) pool = colorPool;
        }

        if (preferredType.HasValue)
        {
            var typed = pool.Where(c => c.Type == preferredType.Value).ToList();
            if (typed.Count > 0) pool = typed;
        }

        var list = pool.ToList();
        if (list.Count == 0) list = available;

        // on regeneration, reorder so the item most visually different from the shown seeds is first
        if (excludedItemIds is { Count: > 0 } && list.Count > 1)
            list = await DiversifyItemsAsync(list, excludedItemIds, ct);

        // no style asked -> default preference order (casual first, formal last).
        if (string.IsNullOrWhiteSpace(intent.Style))
            return list.OrderBy(c => StylePriority(c.Usage)).First();

        // most recent wins (or most-different on regeneration).
        return list.First();
    }

    // minimal context for seed feasibility: weather + avoided colors + hard style clash. No gender lock
    private static OutfitGenerationContext BuildSeedContext(
        PromptIntent intent, IReadOnlyList<string> avoidColors, WeatherData? weather) => new()
        {
            TargetStyle = intent.Style,
            AvoidColors = avoidColors,
            Weather = weather
        };

    // list<ClothingItem> variant of the diversity re-rank used by the fallback path.
    private async Task<List<ClothingItem>> DiversifyItemsAsync(
        List<ClothingItem> items, IReadOnlyCollection<Guid> excludedIds, CancellationToken ct)
    {
        var shown = await clothingRepository.GetByIdsAsync(excludedIds, ct);
        var shownVectors = shown.Where(i => i.Embedding != null).Select(i => i.Embedding!).ToList();
        if (shownVectors.Count == 0) return items;

        return items
            .OrderBy(i => i.Embedding == null ? 1.0 : MaxCosine(i.Embedding, shownVectors))
            .ToList();
    }
}
