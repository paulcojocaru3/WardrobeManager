using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Explaining;

// shared, read-only assembly of the grounded OutfitExplanation from a set of selected items + context.
public static class OutfitExplanationFactory
{
    // evaluator score (in [-1,1]) at/above which a signal is worth surfacing as a positive highlight.
    private const double StrongSignal = 0.6;

    // maps the stable evaluator Name to a short, user-facing phrase. Names come from IOutfitEvaluator.
    private static readonly Dictionary<string, string> HighlightPhrases = new()
    {
        ["Style"] = "fits the requested style",
        ["Weather"] = "suited to the weather",
        ["ColorHarmony"] = "coordinates with the palette",
        ["ColorPreference"] = "matches the requested colors",
        ["PairAffinity"] = "a pairing you've liked before",
    };

    public static async Task<OutfitExplanation> BuildAsync(
        IClothingRepository clothingRepository,
        IWeatherService weatherService,
        IEnumerable<IOutfitEvaluator> evaluators,
        ILogger logger,
        IReadOnlyList<Guid> itemIds,
        string? style,
        string? occasion,
        string? city,
        IReadOnlyList<string>? tradeoffs,
        CancellationToken ct)
    {
        var items = await clothingRepository.GetByIdsAsync(itemIds, ct);
        if (items.Count == 0) return new OutfitExplanation();

        WeatherData? weather = null;
        if (!string.IsNullOrWhiteSpace(city))
        {
            try { weather = await weatherService.GetCurrentWeatherAsync(city, ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Weather unavailable for {City}; explaining without it.", city);
            }
        }

        var context = new OutfitGenerationContext
        {
            TargetStyle = style,
            Occasion = occasion,
            Weather = weather,
            SelectedItems = items.ToList(),
        };

        var pieces = items.Select(item => new OutfitPieceFact
        {
            ItemId = item.Id,
            Slot = SlotLabel(item.Type),
            Name = item.Name,
            Color = item.Color,
            Material = item.Material,
            Season = item.Season,
            SubType = item.SubType,
            Style = item.Usage,
            Description = DescribeItem(item),
            Highlights = HighlightsFor(item, context, evaluators),
        }).ToList();

        return new OutfitExplanation
        {
            Style = style,
            Occasion = occasion,
            Weather = weather,
            Pieces = pieces,
            Tradeoffs = tradeoffs ?? Array.Empty<string>(),
        };
    }

    // runs the soft evaluators for one piece against the rest of the outfit and keeps the strong ones.
    private static IReadOnlyList<string> HighlightsFor(
        ClothingItem item, OutfitGenerationContext context, IEnumerable<IOutfitEvaluator> evaluators)
    {
        var others = context.SelectedItems.Where(i => i.Id != item.Id).ToList();
        var saved = context.SelectedItems;
        context.SelectedItems = others;

        var highlights = new List<string>();
        foreach (var evaluator in evaluators)
        {
            if (highlights.Count >= 2) break; // keep notes concise
            var score = evaluator.Evaluate(item, context);
            if (score is >= StrongSignal && HighlightPhrases.TryGetValue(evaluator.Name, out var phrase))
                highlights.Add(phrase);
        }

        context.SelectedItems = saved;
        return highlights;
    }

    // composes the canonical garment phrase from the CLIP-derived attributes — the textual equivalent of
    private static string DescribeItem(ClothingItem item)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(item.Color)) parts.Add(item.Color!.Trim().ToLowerInvariant());
        if (!string.IsNullOrWhiteSpace(item.Material)) parts.Add(item.Material!.Trim().ToLowerInvariant());
        if (!string.IsNullOrWhiteSpace(item.SubType)) parts.Add(item.SubType!.Trim().ToLowerInvariant());

        var phrase = string.Join(" ", parts).Trim();
        return phrase.Length == 0 ? item.Name : phrase;
    }

    private static string SlotLabel(ClothingType type) => type switch
    {
        ClothingType.Top => "top",
        ClothingType.Bottom => "bottoms",
        ClothingType.Shoes => "shoes",
        ClothingType.Outerwear => "outerwear",
        ClothingType.Accessory => "accessory",
        _ => "item"
    };
}
