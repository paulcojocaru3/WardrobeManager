using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.PlannedOutfits;

public class EventOutfitPlanningService(IClothingRepository clothingRepository) : IEventOutfitPlanningService
{
    // Alert threshold for temperature change (°C) between stored forecast and current weather.
    private const float WeatherTemperatureAlertDeltaCelsius = 5f;

    public (string Style, string Moment) ResolveDayPlan(string eventType, int dayIndex, WeatherData? weather, string? existingMoment = null, List<string>? preferredStyles = null)
    {
        // If the user already provided a moment (e.g. they typed "Dinner", "Gym", "Flight"),
        // try to infer the style directly from that moment string.
        if (!string.IsNullOrWhiteSpace(existingMoment))
        {
            var momentLower = existingMoment.ToLowerInvariant();
            
            var inferredStyle = eventType switch
            {
                _ when momentLower.Contains("gym") || momentLower.Contains("workout") || momentLower.Contains("run") || momentLower.Contains("sport") => "Sports",
                _ when momentLower.Contains("flight") || momentLower.Contains("travel") || momentLower.Contains("airport") || momentLower.Contains("lounging") => "Travel",
                _ when momentLower.Contains("dinner") || momentLower.Contains("party") || momentLower.Contains("club") || momentLower.Contains("evening") || momentLower.Contains("night") => "Party",
                _ when momentLower.Contains("wedding") || momentLower.Contains("ceremony") || momentLower.Contains("gala") => "Formal",
                _ when momentLower.Contains("meeting") || momentLower.Contains("office") || momentLower.Contains("work") || momentLower.Contains("business") => "Business",
                _ when momentLower.Contains("date") || momentLower.Contains("romantic") => "Date",
                _ when momentLower.Contains("brunch") || momentLower.Contains("lunch") || momentLower.Contains("city") || momentLower.Contains("walk") => "Smart Casual",
                _ => GetDefaultStyleForEvent(eventType, weather, preferredStyles)
            };

            return (inferredStyle, existingMoment);
        }

        // If no existing moment, fallback to auto-generation rules
        if (dayIndex == 0)
        {
            return eventType switch
            {
                "Vacation" => ("Travel", "Travel"),
                "Business Trip" => ("Business", "Business"),
                "Wedding" => ("Formal", "Ceremony"),
                "Party" => ("Party", "Evening"),
                "Date" => ("Date", "Evening"),
                "Meeting" => ("Business", "Meeting"),
                "Weekend" => ("Casual", "Leisure"),
                _ => ("Casual", "Day")
            };
        }

        var style = GetDefaultStyleForEvent(eventType, weather, preferredStyles);
        return (style, DetermineMoment(weather));
    }

    private static string GetDefaultStyleForEvent(string eventType, WeatherData? weather, List<string>? preferredStyles)
    {
        // If the user explicitly provided a Vibe/Preferred Styles for this trip, respect the first one available
        if (preferredStyles != null && preferredStyles.Count > 0)
        {
            return preferredStyles.First(); // Prioritize user's vibe
        }

        return eventType switch
        {
            "Wedding" => "Formal",
            "Party" => "Party",
            "Date" => "Date",
            "Meeting" => "Business",
            "Business Trip" => "Business",
            _ => weather?.SeasonSuggestion ?? "Casual"
        };
    }

    public async Task<ClothingItem?> SelectStartItemAsync(
        Guid userId,
        string style,
        WeatherData? weather,
        IReadOnlyCollection<Guid>? excludedItemIds,
        CancellationToken ct)
    {
        var userClothes = await clothingRepository.GetByUserIdAsync(userId, ct);
        var availableClothes = userClothes;

        if (excludedItemIds is { Count: > 0 })
        {
            availableClothes = userClothes.Where(c => !excludedItemIds.Contains(c.Id)).ToList();
            if (availableClothes.Count == 0)
            {
                availableClothes = userClothes;
            }
        }

        if (availableClothes.Count == 0)
        {
            return null;
        }

        var seasonFromWeather = weather?.SeasonSuggestion ?? "Summer";

        var perfectMatch = availableClothes
            .Where(c =>
                !string.IsNullOrEmpty(c.Usage) && c.Usage.Contains(style, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(c.Season) && c.Season.Contains(seasonFromWeather, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (perfectMatch.Count > 0)
        {
            return PickRandom(perfectMatch);
        }

        var seasonMatch = availableClothes
            .Where(c => !string.IsNullOrEmpty(c.Season) && c.Season.Contains(seasonFromWeather, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (seasonMatch.Count > 0)
        {
            return PickRandom(seasonMatch);
        }

        var styleMatch = availableClothes
            .Where(c => !string.IsNullOrEmpty(c.Usage) && c.Usage.Contains(style, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (styleMatch.Count > 0)
        {
            return PickRandom(styleMatch);
        }

        return PickRandom(availableClothes);
    }

    private static string DetermineMoment(WeatherData? weather)
    {
        if (weather == null) return "Day";

        var temp = weather.Temperature;
        var condition = weather.Condition.ToLowerInvariant();

        if (condition.Contains("rain") || condition.Contains("storm")) return "Indoor";
        if (temp > 25) return "Outdoor";
        if (temp < 10) return "Indoor";

        return "Day";
    }

    public static (bool IsSignificantChange, float TemperatureDelta) CompareForecastToCurrentWeather(
        WeatherData? storedForecast,
        WeatherData? currentWeather)
    {
        if (!HasValidTemperature(storedForecast) || !HasValidTemperature(currentWeather))
        {
            return (false, 0f);
        }

        var temperatureDelta = MathF.Abs(currentWeather!.Temperature - storedForecast!.Temperature);
        var isSignificant = temperatureDelta >= WeatherTemperatureAlertDeltaCelsius;

        return (isSignificant, temperatureDelta);
    }

    private static bool HasValidTemperature(WeatherData? weather)
    {
        if (weather == null) return false;

        var temp = weather.Temperature;
        return !float.IsNaN(temp) && !float.IsInfinity(temp);
    }

    private static ClothingItem PickRandom(List<ClothingItem> items)
    {
        var random = new Random();
        return items[random.Next(items.Count)];
    }
}
