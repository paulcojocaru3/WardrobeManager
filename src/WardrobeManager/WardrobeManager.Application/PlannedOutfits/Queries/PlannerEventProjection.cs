using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing.Queries;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.PlannedOutfits.Queries;

// shared lifecycle + projection logic for the planner-event queries (active and archived),
internal static class PlannerEventProjection
{
    // auto-archive events whose end date has passed, and auto-delete events archived over 30 days ago.
    public static async Task ApplyLifecycleTransitionsAsync(
        List<PlannerEvent> events,
        IPlannerEventRepository repository,
        DateTime now,
        CancellationToken ct)
    {
        foreach (var plannerEvent in events.ToList())
        {
            if (plannerEvent.Status == PlannerEvent.ActiveStatus && plannerEvent.EndDate < now.Date)
            {
                plannerEvent.Archive(now);
                await repository.UpdateAsync(plannerEvent, ct);
            }
            else if (plannerEvent.Status == PlannerEvent.ArchivedStatus && plannerEvent.ArchivedAt.HasValue && (now - plannerEvent.ArchivedAt.Value).TotalDays > 30)
            {
                await repository.DeleteAsync(plannerEvent, ct);
                events.Remove(plannerEvent);
            }
        }
    }

    public static PlannerEventDto ToDto(PlannerEvent p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Type = p.Type,
        Location = p.Location,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        Status = p.Status,
        ArchivedAt = p.ArchivedAt,
        PreferredStyles = p.PreferredStyles ?? new List<string>(),
        ReuseAfterDays = p.ReuseAfterDays,
        Itineraries = p.Itineraries.Select(ToItineraryDto).ToList(),
    };

    private static EventItineraryDto ToItineraryDto(EventItinerary i)
    {
        var outfit = i.Outfit is null
            ? null
            : new OutfitDto(
                i.Outfit.Id,
                i.Outfit.Name,
                i.Outfit.IsAiGenerated,
                i.Outfit.IsFavorite,
                i.Outfit.Tags,
                i.Outfit.CreatedAt,
                i.Outfit.Items.Select(ToClothingItemDto).ToList());

        return new EventItineraryDto
        {
            Id = i.Id,
            OutfitId = i.OutfitId,
            Date = i.Date,
            Moment = i.Moment,
            StoredTemperature = i.StoredTemperature,
            Outfit = outfit,
        };
    }

    private static ClothingItemDto ToClothingItemDto(ClothingItem item) => new(
        item.Id,
        item.Name,
        item.Type,
        item.SubType,
        item.Color,
        item.Gender,
        item.Season,
        item.Usage,
        item.ProcessedImageUrl ?? string.Empty,
        item.CreatedAt);
}
