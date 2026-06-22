using System;
using System.Linq;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Scoring;

public sealed class ColorHarmonyEvaluator : IOutfitEvaluator
{
    public string Name => "ColorHarmony";

    public double Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        if (string.IsNullOrWhiteSpace(candidate.Color)) return 1.0;

        double score = 0.5;
        bool candNeutral = ColorFamily.IsNeutral(candidate.Color);
        string candFamily = ColorFamily.FamilyOf(candidate.Color) ?? "";

        var exactMatchBase = context.SelectedItems.FirstOrDefault(i =>
            (i.Type == ClothingType.Top || i.Type == ClothingType.Bottom) &&
            !string.IsNullOrEmpty(i.Color) &&
            i.Color!.Equals(candidate.Color, StringComparison.OrdinalIgnoreCase));

        if (exactMatchBase != null)
        {
            if (candidate.Type == ClothingType.Top || candidate.Type == ClothingType.Bottom) score += 0.4;
            if (candidate.Type == ClothingType.Shoes || candidate.Type == ClothingType.Accessory) score += 0.2;
        }

        var accentFamilies = context.SelectedItems
            .Select(i => ColorFamily.FamilyOf(i.Color))
            .Where(fam => !string.IsNullOrEmpty(fam))
            .Select(fam => fam!)
            .Distinct()
            .ToList();

        if (candNeutral || string.IsNullOrEmpty(candFamily))
        {
            score += 0.2;
        }
        else if (accentFamilies.Contains(candFamily))
        {
            score += 0.15;
        }
        else
        {
            int totalAccents = accentFamilies.Count + 1;
            score += totalAccents switch
            {
                <= 2 => 0.1,
                3 => -0.3,
                _ => -0.6
            };
        }

        // a second strong colour on the piece adds busyness when it brings yet another hue family.
        string? candSecondaryFamily = ColorFamily.FamilyOf(candidate.SecondaryColor);
        if (!string.IsNullOrEmpty(candSecondaryFamily)
            && candSecondaryFamily != candFamily
            && !accentFamilies.Contains(candSecondaryFamily))
        {
            score -= 0.1;
        }

        // two strongly-patterned pieces in one outfit usually clash.
        if (HasStrongPattern(candidate) && context.SelectedItems.Any(HasStrongPattern))
        {
            score -= 0.25;
        }

        double clamped = Math.Clamp(score, -1.0, 1.0);
        return Math.Max(0.05, 0.05 + ((clamped + 1.0) / 2.0) * 1.45);
    }

    // a non-solid pattern (striped, plaid, floral, graphic, ...) reads as a "busy" piece.
    private static bool HasStrongPattern(ClothingItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Pattern)) return false;
        return !item.Pattern.Trim().Equals("solid", StringComparison.OrdinalIgnoreCase);
    }
}
