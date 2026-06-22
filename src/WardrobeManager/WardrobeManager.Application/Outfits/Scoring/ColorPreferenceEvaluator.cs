using System;
using System.Linq;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

public sealed class ColorPreferenceEvaluator : IOutfitEvaluator
{
    public string Name => "ColorPreference";

    public double Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        bool hasDesired = context.DesiredColors.Count > 0;
        bool hasAvoid = context.AvoidColors.Count > 0;
        bool hasPreferred = context.PreferredColors.Count > 0;
        bool hasSoftAvoid = context.SoftAvoidColors.Count > 0;

        if (!hasDesired && !hasAvoid && !hasPreferred && !hasSoftAvoid) return 1.0;

        string color = candidate.Color ?? "";
        if (string.IsNullOrEmpty(color)) return 1.0;

        if (hasAvoid && context.AvoidColors.Any(a => ColorFamily.ColorsMatch(color, a)))
            return 0.2;

        if (hasDesired)
        {
            return context.DesiredColors.Any(d => ColorFamily.ColorsMatch(color, d))
                ? 1.2
                : 0.8;
        }

        if (hasSoftAvoid && context.SoftAvoidColors.Any(a => ColorFamily.ColorsMatch(color, a)))
            return 0.6;

        if (hasPreferred && context.PreferredColors.Any(p => ColorFamily.ColorsMatch(color, p)))
            return 1.1;

        return 1.0;
    }
}
