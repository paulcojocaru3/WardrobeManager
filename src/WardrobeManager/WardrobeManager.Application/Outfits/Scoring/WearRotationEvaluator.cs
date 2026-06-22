using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

public sealed class WearRotationEvaluator : IOutfitEvaluator
{
    public string Name => "WearRotation";

    public double Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        if (context.PreferUnusedItems)
        {
            return ToMultiplier(UnusedScore(candidate, context));
        }

        var score = 0.0;
        var signals = 0;

        if (candidate.IsFavorite)
        {
            score += 0.6;
            signals++;
        }

        if (context.WearRecency.Count > 0)
        {
            score += RecencyScore(candidate, context);
            signals++;
        }

        if (context.WearCounts.Count > 0)
        {
            score += UsageBalanceScore(candidate, context);
            signals++;
        }

        if (context.RecentlyRecommendedItemIds.Contains(candidate.Id))
        {
            score += -0.4;
            signals++;
        }

        return signals == 0 ? 1.0 : ToMultiplier(score / signals);
    }

    private static double UnusedScore(ClothingItem candidate, OutfitGenerationContext context)
    {
        if (!context.WearRecency.TryGetValue(candidate.Id, out var lastWorn))
        {
            return 1.0;
        }

        var days = (DateTime.UtcNow - lastWorn).TotalDays;
        return days switch
        {
            < 7 => -0.85,
            < 30 => -0.3,
            < 90 => 0.3,
            < 180 => 0.7,
            _ => 1.0
        };
    }

    private static double RecencyScore(ClothingItem candidate, OutfitGenerationContext context)
    {
        if (!context.WearRecency.TryGetValue(candidate.Id, out var lastWorn))
        {
            return 0.8;
        }

        var days = (DateTime.UtcNow - lastWorn).TotalDays * context.VarietyDaysFactor;
        return days switch
        {
            < 2 => -0.5,
            < 7 => -0.2,
            < 21 => 0.2,
            < 45 => 0.6,
            _ => 1.0
        };
    }

    private static double UsageBalanceScore(ClothingItem candidate, OutfitGenerationContext context)
    {
        var count = context.WearCounts.TryGetValue(candidate.Id, out var c) ? c : 0;
        var ratio = count / Math.Max(context.MedianWearCount, 1.0);

        return ratio switch
        {
            <= 0.5 => 0.6,
            <= 1.0 => 0.2,
            <= 1.5 => 0.0,
            _ => -0.4
        };
    }

    private static double ToMultiplier(double score) =>
        Math.Max(0.05, 0.05 + ((score + 1.0) / 2.0) * 1.45);
}
