namespace WardrobeManager.Domain.Entities;

public class PlannerEvent
{
    public const string ActiveStatus = "Active";
    public const string ArchivedStatus = "Archived";

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // e.g., "Vacation", "Wedding", "Business Trip"
    public string Location { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = ActiveStatus;
    public DateTime? ArchivedAt { get; set; }
    public List<string> PreferredStyles { get; set; } = new();
    public int? ReuseAfterDays { get; set; } = 3;

    // navigation properties
    public User? User { get; set; }
    public List<EventItinerary> Itineraries { get; set; } = new();

    public static PlannerEvent Create(
        Guid userId,
        string name,
        string type,
        string location,
        DateTime startDate,
        DateTime endDate,
        IEnumerable<string>? preferredStyles,
        DateTime createdAt,
        int? reuseAfterDays = 3)
    {
        return new PlannerEvent
        {
            UserId = userId,
            Name = name,
            Type = type,
            Location = location,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            Status = ActiveStatus,
            PreferredStyles = preferredStyles?.ToList() ?? new List<string>(),
            CreatedAt = createdAt,
            ReuseAfterDays = reuseAfterDays
        };
    }

    public void UpdateDetails(
        string name,
        string type,
        string location,
        DateTime startDate,
        DateTime endDate,
        IEnumerable<string>? preferredStyles,
        int? reuseAfterDays = null)
    {
        Name = name;
        Type = type;
        Location = location;
        StartDate = startDate.Date;
        EndDate = endDate.Date;
        PreferredStyles = preferredStyles?.ToList() ?? new List<string>();
        ReuseAfterDays = reuseAfterDays;
    }

    public void Archive(DateTime archivedAt)
    {
        Status = ArchivedStatus;
        ArchivedAt = archivedAt;
    }
}
