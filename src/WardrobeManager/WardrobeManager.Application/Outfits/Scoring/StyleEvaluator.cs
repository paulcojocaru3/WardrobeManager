using System;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

/// <summary>
/// Scores how well a candidate's formality fits the target style. Exact matches win,
/// truly incompatible pairs are vetoed, and otherwise the score is graded by formality
/// distance: being too casual for the occasion is penalized (so sportswear doesn't slip
/// into a "Smart Casual" dinner outfit), while being slightly dressier is acceptable.
/// </summary>
public class StyleEvaluator : IOutfitEvaluator
{
    public double Weight => 0.30;

    public double Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        if (string.IsNullOrEmpty(context.TargetStyle)) return 0.5;

        string usage = candidate.Usage ?? "";
        if (string.IsNullOrEmpty(usage)) return 0.3; // unknown style -> mild uncertainty

        string target = context.TargetStyle;

        // Hard vetoes: combinations that are never acceptable.
        if (IsHardMismatch(target, usage)) return -1.0;

        // Exact style match.
        if (usage.Contains(target, StringComparison.OrdinalIgnoreCase)) return 1.0;

        // Graded by formality distance. Under-dressing is penalized; over-dressing is fine.
        int delta = FormalityRank(usage) - FormalityRank(target);
        return delta switch
        {
            >= 0 => 0.6,  // same level or dressier than needed -> acceptable
            -1   => 0.1,  // one notch too casual -> tolerated
            _    => -0.5  // two+ notches too casual -> dispreferred (e.g. Sports for a dinner date)
        };
    }

    private static bool IsHardMismatch(string target, string usage)
    {
        bool U(string s) => usage.Contains(s, StringComparison.OrdinalIgnoreCase);

        if (target.Equals("Formal", StringComparison.OrdinalIgnoreCase))
            return U("Sports") || U("Lounge");
        if (target.Equals("Sports", StringComparison.OrdinalIgnoreCase))
            return U("Formal") || U("Party");
        if (target.Equals("Party", StringComparison.OrdinalIgnoreCase))
            return U("Sports");
        return false;
    }

    // Higher = dressier. Used to penalize items that are too casual for the target.
    // Note: check "Smart Casual" before "Casual" (substring) and likewise for Sports.
    private static int FormalityRank(string usage)
    {
        if (usage.Contains("Sports", StringComparison.OrdinalIgnoreCase)) return 0;
        if (usage.Contains("Smart Casual", StringComparison.OrdinalIgnoreCase)) return 2;
        if (usage.Contains("Casual", StringComparison.OrdinalIgnoreCase)) return 1;
        if (usage.Contains("Travel", StringComparison.OrdinalIgnoreCase)) return 1;
        if (usage.Contains("Party", StringComparison.OrdinalIgnoreCase)) return 3;
        if (usage.Contains("Ethnic", StringComparison.OrdinalIgnoreCase)) return 3;
        if (usage.Contains("Formal", StringComparison.OrdinalIgnoreCase)) return 4;
        return 1;
    }
}
