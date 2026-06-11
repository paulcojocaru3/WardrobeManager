using System;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

public sealed class StyleEvaluator : IOutfitEvaluator
{
    public string Name => "Style";
    public double Weight => 0.30;

    public double? Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        bool hasStyle = !string.IsNullOrEmpty(context.TargetStyle);
        if (!hasStyle && context.Formality == null) return null; // nothing to judge

        string usage = candidate.Usage;
        if (usage == null)
        {
            usage = "";
        }

        double? styleScore = null;
        if (hasStyle)
        {
            string target = context.TargetStyle!;
            if (string.IsNullOrEmpty(usage))
            {
                styleScore = 0.3; // unknown style -> mild uncertainty
            }
            else if (IsHardMismatch(target, usage))
            {
                return -1.0; // never acceptable (e.g. Sports in a Formal outfit)
            }
            else if (usage.Contains(target, StringComparison.OrdinalIgnoreCase))
            {
                styleScore = 1.0; // exact match
            }
            else
            {
                int distance = Math.Abs(FormalityRank(usage) - FormalityRank(target));
                styleScore = distance switch
                {
                    0 => 0.8,   // same formality level, different label
                    1 => 0.6,   // adjacent style (e.g. smart casual for a casual ask) -> fine, small dip
                    2 => 0.1,   // two notches off -> dispreferred
                    _ => -0.3   // far apart (e.g. formal for a casual ask) -> penalized
                };
            }
        }

        // Secondary: explicit formality 1–5 (mapped to rank 0–4).
        double? formalityScore = null;
        if (context.Formality is int f && !string.IsNullOrEmpty(usage))
        {
            int desiredRank = Math.Clamp(f - 1, 0, 4);
            int diff = Math.Abs(FormalityRank(usage) - desiredRank);
            formalityScore = diff switch { 0 => 1.0, 1 => 0.5, 2 => 0.0, _ => -0.4 };
        }

        if (styleScore.HasValue && formalityScore.HasValue)
            return 0.7 * styleScore.Value + 0.3 * formalityScore.Value;
        if (styleScore.HasValue)
        {
            return styleScore;
        }
        return formalityScore;
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

    // Higher = dressier. Check "Smart Casual" before "Casual" (substring), likewise Sports.
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
