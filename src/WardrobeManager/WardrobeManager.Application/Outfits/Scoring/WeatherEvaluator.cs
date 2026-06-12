using System;
using System.Linq;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Scoring;

public sealed class WeatherEvaluator : IOutfitEvaluator
{
    public string Name => "Weather";
    public double Weight => 0.40; // weight only counts when weather is actually known

    public double? Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        // No live weather -> abstain entirely so it doesn't dilute style/color signals.
        if (context.Weather == null) return null;

        // Hard rule: No shorts/sandals if freezing (< 10C). Prefer the reliable SubType, fall back to Name.
        string usage = candidate.Usage;
        if (usage == null)
        {
            usage = "";
        }

        string name = candidate.Name;

        string subType = candidate.SubType;
        if (subType == null)
        {
            subType = "";
        }
        bool isWarmOnlyGarment =
            subType is "shorts" or "sandals" or "flip flops" ||
            name.Contains("shorts", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("sandals", StringComparison.OrdinalIgnoreCase);
        if (context.Weather.Temperature < 10 && isWarmOnlyGarment)
        {
            return -1.0; // Veto
        }

        double score = 0.5;
        string season = candidate.Season;
        if (season == null)
        {
            season = "";
        }

        bool isAllSeasons = season.Contains("All Seasons", StringComparison.OrdinalIgnoreCase);
        if (isAllSeasons || season.Contains(context.Weather.SeasonSuggestion, StringComparison.OrdinalIgnoreCase))
        {
            score += 0.5; // Direct season match or universal all-seasons item
        }
        else if (!string.IsNullOrEmpty(season))
        {
            // If it's cold (Winter/Autumn) but the candidate is Summer
            if (context.Weather.Temperature < 15 && season.Contains("Summer", StringComparison.OrdinalIgnoreCase))
            {
                if (candidate.Type == ClothingType.Top)
                {
                    score -= 0.1; // Slight penalty but allowed for layering
                }
                else
                {
                    score -= 0.8; // Heavy penalty for summer bottoms/shoes in winter
                }
            }
            // If it's hot (Summer) but the candidate is Winter
            else if (context.Weather.Temperature > 22 && season.Contains("Winter", StringComparison.OrdinalIgnoreCase))
            {
                return -1.0; // Veto winter items in hot weather
            }
            else
            {
                score -= 0.2; // General penalty for wrong season
            }
        }

        // Boost for rain gear
        if (context.Weather.Condition.Contains("Rain", StringComparison.OrdinalIgnoreCase) &&
            (candidate.Type == ClothingType.Outerwear || candidate.Type == ClothingType.Shoes))
        {
            if (usage.Contains("Rain", StringComparison.OrdinalIgnoreCase) || name.Contains("boots", StringComparison.OrdinalIgnoreCase) || name.Contains("rain", StringComparison.OrdinalIgnoreCase))
            {
                score += 0.4;
            }
        }

        return Math.Clamp(score, -1.0, 1.0);
    }
}
