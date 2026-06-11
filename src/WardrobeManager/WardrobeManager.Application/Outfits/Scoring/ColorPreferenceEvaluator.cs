using System;
using System.Linq;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

public sealed class ColorPreferenceEvaluator : IOutfitEvaluator
{
    public string Name => "ColorPreference";
    public double Weight => 0.20;

    public double? Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        bool hasDesired = context.DesiredColors.Count > 0;
        bool hasAvoid = context.AvoidColors.Count > 0;
        bool hasPreferred = context.PreferredColors.Count > 0;

        if (!hasDesired && !hasAvoid && !hasPreferred) return null; // abstain

        string color = candidate.Color;
        if (color == null)
        {
            color = "";
        }
        if (string.IsNullOrEmpty(color)) return null;

        // Explicit avoid -> hard veto.
        if (hasAvoid && context.AvoidColors.Any(a => ColorFamily.ColorsMatch(color, a)))
            return -1.0;

        // Explicit desired colors take precedence over favorites.
        if (hasDesired)
        {
            return context.DesiredColors.Any(d => ColorFamily.ColorsMatch(color, d))
                ? 1.0    // exactly what the user asked for
                : -0.3;  // wanted specific colors, this isn't one
        }

        // Soft favorite-color nudge: reward matches, never penalize non-matches.
        if (hasPreferred && context.PreferredColors.Any(p => ColorFamily.ColorsMatch(color, p)))
            return 0.5;

        return null; // avoid-only/preferred-only with no match -> neutral abstain
    }
}
