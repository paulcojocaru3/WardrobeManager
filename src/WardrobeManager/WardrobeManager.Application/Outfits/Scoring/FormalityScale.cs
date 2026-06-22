using System;
using System.Collections.Generic;
using System.Linq;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

// single source of truth for the 0..4 formality rank, shared by StyleEvaluator and
public static class FormalityScale
{
    // gemma3 formality (1..5) -> rank (0..4).
    public static int RankOfFormalityLevel(int formality) => Math.Clamp(formality - 1, 0, 4);

    // usage label -> rank (0..4). Mirrors the historical StyleEvaluator.FormalityRank mapping.
    public static int RankOfUsage(string? usage)
    {
        usage ??= "";
        if (usage.Contains("Sports", StringComparison.OrdinalIgnoreCase)) return 0;
        if (usage.Contains("Smart Casual", StringComparison.OrdinalIgnoreCase)) return 2;
        if (usage.Contains("Casual", StringComparison.OrdinalIgnoreCase)) return 1;
        if (usage.Contains("Travel", StringComparison.OrdinalIgnoreCase)) return 1;
        if (usage.Contains("Party", StringComparison.OrdinalIgnoreCase)) return 3;
        if (usage.Contains("Ethnic", StringComparison.OrdinalIgnoreCase)) return 3;
        if (usage.Contains("Formal", StringComparison.OrdinalIgnoreCase)) return 4;
        return 1; // default to Casual
    }

    // the item's rank, preferring the finer per-item Formality, else parsing Usage. Returns null only
    public static int? TryRankOf(ClothingItem item)
    {
        if (item.Formality.HasValue) return RankOfFormalityLevel(item.Formality.Value);
        if (!string.IsNullOrWhiteSpace(item.Usage)) return RankOfUsage(item.Usage);
        return null;
    }

    // non-nullable convenience: falls back to Casual (1) when the item has no signal.
    public static int RankOf(ClothingItem item) => TryRankOf(item) ?? 1;

    // median rank over the items that carry a known formality signal; null when none do.
    public static int? MedianKnownRank(IEnumerable<ClothingItem> items)
    {
        var ranks = items.Select(TryRankOf).Where(r => r.HasValue).Select(r => r!.Value).OrderBy(r => r).ToList();
        if (ranks.Count == 0) return null;
        return ranks[ranks.Count / 2];
    }
}
