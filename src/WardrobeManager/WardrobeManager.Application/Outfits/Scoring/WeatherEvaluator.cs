using System;
using WardrobeManager.Application.Outfits.Feasibility;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Scoring;

// stage 2 (soft) weather scoring only. The hard rules - warm-only garments when freezing, winter items
public sealed class WeatherEvaluator(IThermalRules thermal) : IOutfitEvaluator
{
    public string Name => "Weather";

    // temperature (°C) at which the "lighter on hot days" boost reaches full strength.
    private const double HotFullC = 32.0;
    private const double MaxLightBoost = 0.4;

    // rain-gear boost at full rain probability.
    private const double MaxRainBoost = 0.5;

    // keywords that mark a clearly light / short-cut garment.
    private static readonly string[] LightTopHints =
        ["tshirt", "t-shirt", "tee", "tank", "sleeveless", "polo", "cami", "crop"];

    // materials that insulate vs. breathe — used to nudge the score by the gemma3 material attribute.
    private static readonly string[] WarmMaterials =
        ["wool", "cashmere", "fleece", "leather", "suede", "knit", "corduroy"];
    private static readonly string[] CoolMaterials =
        ["linen", "cotton", "mesh", "chambray"];

    public double Evaluate(ClothingItem candidate, OutfitGenerationContext context)
    {
        // no live weather -> neutral multiplier
        if (context.Weather == null) return 1.0;

        string usage = candidate.Usage ?? "";
        string name = candidate.Name ?? "";

        double score = 0.5;
        string season = candidate.Season ?? "";

        double perceived = context.Weather.FeelsLike ?? context.Weather.Temperature;

        bool isAllSeasons = season.Contains("All Seasons", StringComparison.OrdinalIgnoreCase);
        if (isAllSeasons || season.Contains(context.Weather.SeasonSuggestion, StringComparison.OrdinalIgnoreCase))
        {
            score += 0.5; // Direct season match or universal all-seasons item
        }
        else if (!string.IsNullOrEmpty(season))
        {
            // cold weather but the candidate is a Summer piece.
            if (perceived < thermal.ColdC && season.Contains("Summer", StringComparison.OrdinalIgnoreCase))
            {
                score += candidate.Type == ClothingType.Top
                    ? -0.1  // tops can still layer
                    : -0.8; // summer bottoms/shoes in the cold
            }
            // hot weather but the candidate is a Winter piece
            else if (perceived > thermal.HotC && season.Contains("Winter", StringComparison.OrdinalIgnoreCase))
            {
                score -= 0.8;
            }
            else
            {
                score -= 0.2; // General penalty for wrong season
            }
        }

        if (context.PreferLightOnHotDays && perceived > thermal.HotC && IsLightGarment(candidate))
        {
            double warmth = Math.Clamp((perceived - thermal.HotC) / (HotFullC - thermal.HotC), 0.0, 1.0);
            score += MaxLightBoost * warmth;
        }

        score += MaterialThermalAdjustment(candidate.Material, perceived);

        if (candidate.Type == ClothingType.Outerwear || candidate.Type == ClothingType.Shoes)
        {
            double rainFactor = RainFactor(context.Weather);
            bool rainReady = usage.Contains("Rain", StringComparison.OrdinalIgnoreCase)
                || name.Contains("boots", StringComparison.OrdinalIgnoreCase)
                || name.Contains("rain", StringComparison.OrdinalIgnoreCase);
            if (rainFactor > 0 && rainReady)
            {
                score += MaxRainBoost * rainFactor;
            }
        }

        double clamped = Math.Clamp(score, -1.0, 1.0);
        double normalized = (clamped + 1.0) / 2.0; // [0, 1]

        // return a multiplier in [0.05, 1.5]
        return Math.Max(0.05, 0.05 + normalized * 1.45);
    }

    // nudges the score by the garment's material: warm fabrics help in the cold and hurt in the heat,
    private double MaterialThermalAdjustment(string? material, double perceived)
    {
        if (string.IsNullOrWhiteSpace(material)) return 0.0;

        bool warm = WarmMaterials.Any(m => material.Contains(m, StringComparison.OrdinalIgnoreCase));
        bool cool = CoolMaterials.Any(m => material.Contains(m, StringComparison.OrdinalIgnoreCase));

        if (perceived <= thermal.ColdC)
        {
            if (warm) return 0.2;
            if (cool) return -0.15;
        }
        else if (perceived >= thermal.HotC)
        {
            if (warm) return -0.25;
            if (cool) return 0.15;
        }
        return 0.0;
    }

    private static double RainFactor(WardrobeManager.Application.Abstractions.WeatherData weather)
    {
        if (weather.RainChance.HasValue)
            return Math.Clamp(weather.RainChance.Value / 100.0, 0.0, 1.0);

        string condition = weather.Condition ?? "";
        bool rainy = condition.Contains("Rain", StringComparison.OrdinalIgnoreCase)
            || condition.Contains("Drizzle", StringComparison.OrdinalIgnoreCase)
            || condition.Contains("Thunderstorm", StringComparison.OrdinalIgnoreCase);
        return rainy ? 0.8 : 0.0;
    }

    private static bool IsLightGarment(ClothingItem item)
    {
        string subType = item.SubType ?? "";
        string name = item.Name ?? "";

        if (item.Type == ClothingType.Bottom)
        {
            return subType.Contains("short", StringComparison.OrdinalIgnoreCase)
                || name.Contains("short", StringComparison.OrdinalIgnoreCase);
        }

        if (item.Type == ClothingType.Top)
        {
            return LightTopHints.Any(h =>
                subType.Contains(h, StringComparison.OrdinalIgnoreCase) ||
                name.Contains(h, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }
}
