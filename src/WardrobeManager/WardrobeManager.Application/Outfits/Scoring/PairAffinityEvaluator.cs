using System;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

// rewards candidates that the user has historically worn well with the items already in the
public sealed class PairAffinityEvaluator : IOutfitEvaluator
{
    public string Name => "PairAffinity";

    public double Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        if (context.PairCompatibility.Count == 0 || context.SelectedItems.Count == 0) return 1.0;

        double sum = 0;
        var known = 0;
        foreach (var selected in context.SelectedItems)
        {
            if (selected.Id == candidate.Id) continue;
            if (context.PairCompatibility.TryGetValue(ItemPair.Canonical(candidate.Id, selected.Id), out var compat))
            {
                sum += compat;
                known++;
            }
        }

        if (known == 0) return 1.0;
        
        double normalized = Math.Clamp(sum / known, -1.0, 1.0);
        // multiplier between 0.7 and 1.3
        return 1.0 + (normalized * 0.3);
    }
}
