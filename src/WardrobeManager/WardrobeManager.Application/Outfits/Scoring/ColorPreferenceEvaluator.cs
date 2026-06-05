using System;
using System.Linq;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

/// <summary>
/// Scores candidates against the colors the user explicitly asked for (or to avoid)
/// in their prompt. Stays neutral when the prompt expressed no color preference.
/// </summary>
public class ColorPreferenceEvaluator : IOutfitEvaluator
{
    public double Weight => 0.20;

    public double Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        bool hasDesired = context.DesiredColors.Count > 0;
        bool hasAvoid = context.AvoidColors.Count > 0;

        // No color intent in the prompt -> neutral, doesn't disturb ranking.
        if (!hasDesired && !hasAvoid) return 0.0;

        string color = candidate.Color ?? "";
        if (string.IsNullOrEmpty(color)) return 0.0;

        if (hasAvoid && context.AvoidColors.Any(a => ColorsMatch(color, a)))
            return -1.0; // Veto colors the user explicitly rejected

        if (hasDesired && context.DesiredColors.Any(d => ColorsMatch(color, d)))
            return 1.0; // Exactly what the user asked for

        // User wanted specific colors and this isn't one of them.
        if (hasDesired) return -0.3;

        return 0.0;
    }

    private static bool ColorsMatch(string itemColor, string promptColor)
    {
        return itemColor.Contains(promptColor, StringComparison.OrdinalIgnoreCase)
            || promptColor.Contains(itemColor, StringComparison.OrdinalIgnoreCase);
    }
}
