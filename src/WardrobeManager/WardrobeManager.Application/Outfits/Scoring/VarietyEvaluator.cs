using System;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

public sealed class VarietyEvaluator : IOutfitEvaluator
{
    public string Name => "Variety";
    public double Weight => 0.10;

    public double? Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        double? favorite = candidate.IsFavorite ? 0.6 : (double?)null;

        double? recency = null;
        if (context.WearRecency.Count > 0)
        {
            if (context.WearRecency.TryGetValue(candidate.Id, out var lastWorn))
            {
                var days = (DateTime.UtcNow - lastWorn).TotalDays;
                recency = days switch
                {
                    < 2 => -0.5,   // worn today/yesterday -> let it rest
                    < 7 => -0.2,
                    < 21 => 0.2,
                    < 45 => 0.6,
                    _ => 1.0       // not worn in 6+ weeks -> bring it back
                };
            }
            else
            {
                recency = 0.8; // never worn -> rediscover
            }
        }

        if (favorite.HasValue && recency.HasValue)
            return 0.5 * favorite.Value + 0.5 * recency.Value;

        // possibly null -> abstain
        if (favorite.HasValue)
        {
            return favorite;
        }
        return recency;
    }
}
