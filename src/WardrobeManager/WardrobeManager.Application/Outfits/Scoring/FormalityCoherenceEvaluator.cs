using System;
using System.Linq;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

// cross-item formality coherence — the fix for "formal pants + casual print tee + leather slippers" and for
public sealed class FormalityCoherenceEvaluator : IOutfitEvaluator
{
    public string Name => "FormalityCoherence";

    public double Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        var candRank = FormalityScale.TryRankOf(candidate);
        if (candRank is null) return 1.0; // un-enriched candidate -> abstain

        int anchorRank;
        if (context.OutfitFormalityRank is int outfitRank)
        {
            anchorRank = outfitRank;
        }
        else
        {
            var median = FormalityScale.MedianKnownRank(context.SelectedItems);
            if (median is null) return 1.0; // nothing to anchor to -> abstain
            anchorRank = median.Value;
        }

        int diff = Math.Abs(candRank.Value - anchorRank);

        // only >1 level apart is penalized: adjacent levels (smart-casual + casual) are fine.
        double score = diff switch
        {
            0 => 1.0,
            1 => 0.3,
            2 => -0.5,
            3 => -0.85,
            _ => -1.0
        };

        double clamped = Math.Clamp(score, -1.0, 1.0);
        return Math.Max(0.05, 0.05 + ((clamped + 1.0) / 2.0) * 1.45);
    }
}
