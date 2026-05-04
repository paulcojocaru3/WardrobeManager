using System;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Scoring;

public class WeatherEvaluator : IOutfitEvaluator
{
    public double Weight => 0.40; // 40% weight for weather

    public double Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        if (context.Weather == null) return 0.5; // Neutral

        // Hard rule: No outerwear if it's hot (> 23C)
        if (context.Weather.Temperature > 23 && candidate.Type == ClothingType.Outerwear)
        {
            return -1.0; // Veto
        }

        // Hard rule: No shorts/sandals if freezing (< 5C)
        string usage = candidate.Usage ?? "";
        string name = candidate.Name ?? "";
        if (context.Weather.Temperature < 5 && 
            (name.Contains("shorts", StringComparison.OrdinalIgnoreCase) || 
             name.Contains("sandals", StringComparison.OrdinalIgnoreCase)))
        {
            return -1.0; // Veto
        }

        double score = 0.5;
        string season = candidate.Season ?? "";

        if (season.Contains(context.Weather.SeasonSuggestion, StringComparison.OrdinalIgnoreCase))
        {
            score += 0.5;
        }
        else if (!string.IsNullOrEmpty(season))
        {
            score -= 0.3; // Penalty for wrong season
        }

        // Boost for rain gear
        if (context.Weather.Condition.Contains("Rain", StringComparison.OrdinalIgnoreCase) && candidate.Type == ClothingType.Outerwear)
        {
            score += 0.2;
        }

        return Math.Clamp(score, -1.0, 1.0);
    }
}
