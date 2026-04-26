namespace WardrobeManager.Domain.Entities;

public class PlannerEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // e.g., "Vacation", "Wedding", "Business Trip"
    public string Location { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Active"; // "Active" or "Archived"
    public DateTime? ArchivedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public List<EventItinerary> Itineraries { get; set; } = new();
}