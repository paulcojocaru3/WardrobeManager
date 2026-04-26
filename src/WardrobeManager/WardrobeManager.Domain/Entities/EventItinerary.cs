namespace WardrobeManager.Domain.Entities;

public class EventItinerary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlannerEventId { get; set; }
    public Guid OutfitId { get; set; }
    public DateTime Date { get; set; }
    public string Moment { get; set; } = string.Empty; // e.g., "Morning", "Dinner", "Flight"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public PlannerEvent? PlannerEvent { get; set; }
    public Outfit? Outfit { get; set; }
}