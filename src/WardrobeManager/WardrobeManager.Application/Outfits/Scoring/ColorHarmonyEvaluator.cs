using System;
using System.Linq;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Scoring;

public sealed class ColorHarmonyEvaluator : IOutfitEvaluator
{
    public string Name => "ColorHarmony";
    public double Weight => 0.20;

    public double? Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        if (string.IsNullOrWhiteSpace(candidate.Color)) return null; // abstain

        double score = 0.5;
        bool candNeutral = ColorFamily.IsNeutral(candidate.Color);
        string? candFamily = ColorFamily.FamilyOf(candidate.Color);

        // Co-ord / set bonus: a Top & Bottom sharing the exact color reads as deliberate.
        var exactMatchBase = context.SelectedItems.FirstOrDefault(i =>
            (i.Type == ClothingType.Top || i.Type == ClothingType.Bottom) &&
            !string.IsNullOrEmpty(i.Color) &&
            i.Color!.Equals(candidate.Color, StringComparison.OrdinalIgnoreCase));

        if (exactMatchBase != null)
        {
            if (candidate.Type == ClothingType.Top || candidate.Type == ClothingType.Bottom) score += 0.4;
            if (candidate.Type == ClothingType.Shoes || candidate.Type == ClothingType.Accessory) score += 0.2;
        }

        // Hue-family discipline over the accent colors already chosen.
        var accentFamilies = context.SelectedItems
            .Select(i => ColorFamily.FamilyOf(i.Color))
            .Where(fam => fam != null)
            .Select(fam => fam!)
            .Distinct()
            .ToList();

        if (candNeutral || candFamily == null)
        {
            score += 0.2; // neutral pairs with anything
        }
        else if (accentFamilies.Contains(candFamily))
        {
            score += 0.15; // same accent family -> cohesive / analogous
        }
        else
        {
            int totalAccents = accentFamilies.Count + 1; // a new accent family
            score += totalAccents switch
            {
                <= 2 => 0.1,  // up to two accents -> fine
                3 => -0.3,    // a third strong color -> risky
                _ => -0.6     // clown effect
            };
        }

        return Math.Clamp(score, -1.0, 1.0);
    }
}
