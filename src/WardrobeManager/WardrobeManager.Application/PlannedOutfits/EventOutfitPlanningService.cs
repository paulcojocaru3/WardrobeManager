using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.PlannedOutfits;

public class EventOutfitPlanningService(IClothingRepository clothingRepository) : IEventOutfitPlanningService
{
    public (string Style, string Moment) ResolveDayPlan(string eventType, int dayIndex, WeatherData? weather)
    {
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

        var style = eventType switch
        {
            "Wedding" => "Formal",
            "Party" => "Party",
            "Date" => "Date",
            "Meeting" => "Business",
            _ => weather?.SeasonSuggestion ?? "Casual"
        };

        return (style, DetermineMoment(weather));
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

    private static ClothingItem PickRandom(List<ClothingItem> items)
    {
        var random = new Random();
        return items[random.Next(items.Count)];
    }
}
