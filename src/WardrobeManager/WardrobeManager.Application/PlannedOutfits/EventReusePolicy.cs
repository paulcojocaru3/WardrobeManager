using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.PlannedOutfits;

public static class EventReusePolicy
{
    public static HashSet<Guid> ComputeExcludedItemIds(
        IReadOnlyDictionary<DateTime, IReadOnlyCollection<ClothingItem>> usageByDate,
        DateTime targetDate,
        int? reuseAfterDays)
    {
        var excluded = new HashSet<Guid>();
        var target = targetDate.Date;

        foreach (var (date, items) in usageByDate)
        {
            var distance = Math.Abs((target - date.Date).Days);
            if (reuseAfterDays.HasValue && distance >= reuseAfterDays.Value)
            {
                continue;
            }

            excluded.UnionWith(items
                .Where(item => item.Type is ClothingType.Top or ClothingType.Bottom)
                .Select(item => item.Id));
        }

        return excluded;
    }

    public static Dictionary<DateTime, IReadOnlyCollection<ClothingItem>> BuildUsageMap(
        IEnumerable<EventItinerary> itineraries)
        => itineraries
            .Where(itinerary => itinerary.Outfit != null)
            .GroupBy(itinerary => itinerary.Date.Date)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<ClothingItem>)group
                    .SelectMany(itinerary => itinerary.Outfit!.Items)
                    .GroupBy(item => item.Id)
                    .Select(items => items.First())
                    .ToList());
}
