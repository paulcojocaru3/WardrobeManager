using System;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

// applies the user's learned color/style taste as a gentle nudge. Kept at a low weight and below
public sealed class TasteProfileEvaluator : IOutfitEvaluator
{
    public string Name => "Taste";

    public double Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        if (context.LearnedColorScores.Count == 0 && context.LearnedStyleScores.Count == 0) return 1.0;

        double sum = 0;
        var known = 0;

        var colorKey = TasteKey.Color(candidate.Color);
        if (colorKey != null && context.LearnedColorScores.TryGetValue(colorKey, out var colorScore))
        {
            sum += colorScore;
            known++;
        }

        var styleKey = TasteKey.Style(candidate.Usage);
        if (styleKey != null && context.LearnedStyleScores.TryGetValue(styleKey, out var styleScore))
        {
            sum += styleScore;
            known++;
        }

        if (known == 0) return 1.0;

        double normalized = Math.Max(-1.0, Math.Min(1.0, (sum / known - 0.5) * 2.0));
        return 1.0 + (normalized * 0.15); // Returns multiplier between 0.85 and 1.15
    }
}
