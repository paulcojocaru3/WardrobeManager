using WardrobeManager.Application.Outfits.Queries;

namespace WardrobeManager.Application.PlannedOutfits.Queries;

public sealed class EventItineraryDto
{
    public Guid Id { get; set; }
    public Guid OutfitId { get; set; }
    public DateTime Date { get; set; }
    public string Moment { get; set; } = string.Empty;
    public float? StoredTemperature { get; set; }
    public OutfitDto Outfit { get; set; } = null!;
}

public sealed class PlannerEventDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ArchivedAt { get; set; }
    public List<EventItineraryDto> Itineraries { get; set; } = new();
    public List<string> PreferredStyles { get; set; } = new();
}