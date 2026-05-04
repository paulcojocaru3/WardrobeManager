using System;
using System.Linq;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

public class ColorHarmonyEvaluator : IOutfitEvaluator
{
    public double Weight => 0.15; // 15% weight for color harmony

    public double Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        if (string.IsNullOrEmpty(candidate.Color)) return 0.5;

        double score = 0.8; // Generally good unless clashing

        // Simple harmony logic: Avoid monochrome unless intentional set, neutral colors are safe.
        string[] neutrals = { "black", "white", "gray", "grey", "navy", "beige", "brown", "tan" };
        bool isCandidateNeutral = neutrals.Any(n => candidate.Color.Contains(n, StringComparison.OrdinalIgnoreCase));

        int sameColorCount = 0;
        foreach (var item in context.SelectedItems)
        {
            if (string.IsNullOrEmpty(item.Color)) continue;

            if (item.Color.Equals(candidate.Color, StringComparison.OrdinalIgnoreCase))
            {
                sameColorCount++;
            }
        }

        // Penalize if we have too much of the same non-neutral color
        if (sameColorCount > 0 && !isCandidateNeutral)
        {
            score -= (0.4 * sameColorCount); // Significant penalty for each matching non-neutral color to promote variety
        }
        else if (isCandidateNeutral && sameColorCount == 0)
        {
            score += 0.2; // Boost neutral colors slightly when they contrast
        }

        return Math.Clamp(score, -1.0, 1.0);
    }
}
