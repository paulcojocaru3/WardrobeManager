using System;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

public class StyleEvaluator : IOutfitEvaluator
{
    public double Weight => 0.30; // 30% weight for style

    public double Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        if (string.IsNullOrEmpty(context.TargetStyle)) return 0.5;

        string usage = candidate.Usage ?? "";
        if (string.IsNullOrEmpty(usage)) return 0.3; // Penalty for unknown style

        // Veto mismatches
        if (context.TargetStyle.Equals("Formal", StringComparison.OrdinalIgnoreCase))
        {
            if (usage.Contains("Sports", StringComparison.OrdinalIgnoreCase) || usage.Contains("Lounge", StringComparison.OrdinalIgnoreCase))
                return -1.0;
        }

        if (context.TargetStyle.Equals("Sports", StringComparison.OrdinalIgnoreCase))
        {
            if (usage.Contains("Formal", StringComparison.OrdinalIgnoreCase) || usage.Contains("Party", StringComparison.OrdinalIgnoreCase))
                return -1.0;
        }

        if (usage.Contains(context.TargetStyle, StringComparison.OrdinalIgnoreCase))
        {
            return 1.0; // Perfect match
        }

        return 0.5; // Neutral
    }
}
