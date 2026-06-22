namespace WardrobeManager.Domain.Entities;

public class EventItinerary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlannerEventId { get; set; }
    public Guid OutfitId { get; set; }
    public DateTime Date { get; set; }
    public string Moment { get; set; } = string.Empty; // e.g., "Morning", "Dinner", "Flight"
    public float? StoredTemperature { get; set; } // The temperature forecast when the outfit was planned
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // navigation properties
    public PlannerEvent? PlannerEvent { get; set; }
    public Outfit? Outfit { get; set; }

    public static EventItinerary Create(
        Guid plannerEventId,
        Guid outfitId,
        DateTime date,
        string moment,
        float? storedTemperature,
        DateTime createdAt)
    {
        return new EventItinerary
        {
            PlannerEventId = plannerEventId,
            OutfitId = outfitId,
            Date = date.Date,
            Moment = moment,
            StoredTemperature = storedTemperature,
            CreatedAt = createdAt
        };
    }

    public void UpdateDetails(Guid outfitId, DateTime date, string moment, float? storedTemperature)
    {
        OutfitId = outfitId;
        Date = date.Date;
        Moment = moment;
        StoredTemperature = storedTemperature;
    }

    public void AssignOutfit(Guid outfitId, float? storedTemperature)
    {
        OutfitId = outfitId;
        StoredTemperature = storedTemperature;
    }
}
