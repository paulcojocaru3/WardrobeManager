using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Abstractions;

public interface IEventOutfitPlanningService
{
    (string Style, string Moment) ResolveDayPlan(string eventType, int dayIndex, WeatherData? weather, string? existingMoment = null, List<string>? preferredStyles = null);
    Task<ClothingItem?> SelectStartItemAsync(
        Guid userId,
        string style,
        WeatherData? weather,
        IReadOnlyCollection<Guid>? excludedItemIds,
        CancellationToken ct);
}
