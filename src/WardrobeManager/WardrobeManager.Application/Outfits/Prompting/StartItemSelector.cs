using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Prompting;

/// <summary>
/// Picks the seed item for outfit generation. Hybrid strategy: Fashion-CLIP embeddings
/// rank wardrobe items by VISUAL similarity to the described garment (so "white shirt"
/// finds the shirt-shaped white top without needing explicit subtypes), then the
/// requested style narrows that ranked pool by CONTEXT via the Usage field (so "elegant"
/// prefers formal-ish items). Falls back to style/type filtering when no embedding.
/// </summary>
public class StartItemSelector(IClothingRepository clothingRepository, IMlService mlService) : IStartItemSelector
{
    public async Task<ClothingItem?> SelectAsync(Guid userId, PromptIntent intent, CancellationToken ct = default)
    {
        ClothingType? preferredType = intent.RequestedTypes.Count > 0 ? intent.RequestedTypes[0] : null;

        string? queryText = BuildQueryText(intent);
        if (!string.IsNullOrWhiteSpace(queryText))
        {
            float[] vector;
            try { vector = await mlService.EmbedTextAsync(queryText, ct); }
            catch { vector = Array.Empty<float>(); }

            if (vector.Length > 0)
            {
                // Visual ranking: top candidates by cosine similarity within the requested type.
                var pool = await clothingRepository.GetSimilarItemsAsync(
                    userId, vector, preferredType, limit: 20, threshold: null, ct: ct);

                // If nothing in that type, retry across all types.
                if (pool.Count == 0 && preferredType.HasValue)
                    pool = await clothingRepository.GetSimilarItemsAsync(
                        userId, vector, null, limit: 20, threshold: null, ct: ct);

                var best = PickBest(pool, intent.Style);
                if (best != null) return best;
            }
        }

        return await FallbackSelectAsync(userId, intent, preferredType, ct);
    }

    // Embedding already ordered the pool by visual similarity (best first). When a style
    // is known, prefer the best-ranked item whose Usage fits that context (e.g. "Formal");
    // otherwise keep the top visual match so we never return nothing.
    private static ClothingItem? PickBest(IReadOnlyList<(ClothingItem Item, double Similarity)> pool, string? style)
    {
        if (pool.Count == 0) return null;

        if (!string.IsNullOrWhiteSpace(style))
        {
            foreach (var (item, _) in pool)
            {
                if (item.Usage != null && item.Usage.Contains(style!, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
        }

        return pool[0].Item;
    }

    private static string? BuildQueryText(PromptIntent intent)
    {
        // An explicitly described garment is the strongest anchor.
        if (!string.IsNullOrWhiteSpace(intent.AnchorDescription))
        {
            var anchor = intent.AnchorDescription!.Trim();
            // Fold in any requested colors the model left out of the phrase.
            var missing = intent.DesiredColors
                .Where(c => !anchor.Contains(c, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return missing.Count > 0 ? $"{string.Join(" ", missing)} {anchor}" : anchor;
        }

        var parts = new List<string>();
        parts.AddRange(intent.DesiredColors);
        if (!string.IsNullOrWhiteSpace(intent.Style)) parts.Add(intent.Style!);
        if (!string.IsNullOrWhiteSpace(intent.Occasion)) parts.Add(intent.Occasion!);

        var text = string.Join(" ", parts).Trim();
        return string.IsNullOrWhiteSpace(text) ? null : $"a {text} clothing item";
    }

    private async Task<ClothingItem?> FallbackSelectAsync(
        Guid userId, PromptIntent intent, ClothingType? preferredType, CancellationToken ct)
    {
        var all = await clothingRepository.GetByUserIdAsync(userId, ct);
        if (all.Count == 0) return null;

        IEnumerable<ClothingItem> pool = all;

        if (!string.IsNullOrWhiteSpace(intent.Style))
            pool = pool.Where(c => c.Usage != null && c.Usage.Contains(intent.Style!, StringComparison.OrdinalIgnoreCase));

        if (preferredType.HasValue)
        {
            var typed = pool.Where(c => c.Type == preferredType.Value).ToList();
            if (typed.Count > 0) pool = typed;
        }

        var list = pool.ToList();
        if (list.Count == 0) list = all;

        // GetByUserIdAsync already orders by CreatedAt desc -> most recent wins.
        return list.First();
    }
}
