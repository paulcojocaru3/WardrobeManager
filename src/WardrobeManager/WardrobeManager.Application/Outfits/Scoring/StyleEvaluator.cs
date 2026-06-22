using System;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

public sealed class StyleEvaluator : IOutfitEvaluator
{
    public string Name => "Style";

    public double Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        bool hasStyle = !string.IsNullOrEmpty(context.TargetStyle);
        if (!hasStyle && context.Formality == null) return 1.0;

        string usage = candidate.Usage ?? "";

        double? styleScore = null;
        if (hasStyle)
        {
            string target = context.TargetStyle!;
            if (string.IsNullOrEmpty(usage))
            {
                styleScore = 0.0; // Unknown style -> neutral (neither bonus nor penalty)
            }
            else if (usage.Contains(target, StringComparison.OrdinalIgnoreCase))
            {
                styleScore = 1.0; // Exact match -> max bonus
            }
            else
            {
                int distance = Math.Abs(FormalityScale.RankOfUsage(usage) - FormalityScale.RankOfUsage(target));
                styleScore = distance switch
                {
                    0 => 0.4,   // Same formality level, different label -> slight bonus
                    1 => -0.4,  // Adjacent (e.g. Casual when Sports requested) -> penalty!
                    2 => -0.8,  // Two notches off -> strong penalty
                    _ => -1.0   // Far apart -> soft veto
                };
            }
        }

        double? formalityScore = null;
        if (context.Formality is int f && (candidate.Formality.HasValue || !string.IsNullOrEmpty(usage)))
        {
            int desiredRank = FormalityScale.RankOfFormalityLevel(f);
            // a per-item formality (gemma3, 1..5) is a finer signal than the Usage label; fall back to
            int itemRank = FormalityScale.RankOf(candidate);
            int diff = Math.Abs(itemRank - desiredRank);
            formalityScore = diff switch { 0 => 1.0, 1 => -0.2, 2 => -0.6, _ => -1.0 };
        }

        double score = 0.0;
        if (styleScore.HasValue && formalityScore.HasValue)
            score = 0.7 * styleScore.Value + 0.3 * formalityScore.Value;
        else if (styleScore.HasValue)
            score = styleScore.Value;
        else if (formalityScore.HasValue)
            score = formalityScore.Value;

        double clamped = Math.Clamp(score, -1.0, 1.0);
        return Math.Max(0.05, 0.05 + ((clamped + 1.0) / 2.0) * 1.45);
    }
}
