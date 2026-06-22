namespace WardrobeManager.Domain.Entities;

public class Outfit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAiGenerated { get; set; }

    // links an AI outfit back to the generation that produced it, so later Worn/Favorited
    public Guid? AiGenerationId { get; set; }

    public bool IsEventExclusive { get; set; }
    public bool IsFavorite { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // navigation properties
    public User? User { get; set; }
    public List<ClothingItem> Items { get; set; } = new();
    public List<EventItinerary> EventItineraries { get; set; } = new();

    public static Outfit Create(
        Guid userId,
        string name,
        IEnumerable<ClothingItem> items,
        DateTime createdAt,
        bool isAiGenerated = true,
        bool isEventExclusive = false,
        IEnumerable<string>? tags = null,
        Guid? aiGenerationId = null)
    {
        var outfit = new Outfit
        {
            UserId = userId,
            Name = name,
            IsAiGenerated = isAiGenerated,
            AiGenerationId = aiGenerationId,
            IsEventExclusive = isEventExclusive,
            Tags = tags?.ToList() ?? new List<string>(),
            CreatedAt = createdAt
        };

        outfit.ReplaceItems(items);
        return outfit;
    }

    public void UpdateDetails(string name, IEnumerable<string>? tags, IEnumerable<ClothingItem> items)
    {
        Name = name;
        if (tags != null)
        {
            Tags = tags.ToList();
        }

        ReplaceItems(items);
    }

    public bool ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
        return IsFavorite;
    }

    private void ReplaceItems(IEnumerable<ClothingItem> items)
    {
        var newItems = new List<ClothingItem>();
        foreach (var item in items)
        {
            if (newItems.Any(i => i.Type == item.Type))
            {
                throw new InvalidOperationException($"Outfit already contains an item of type {item.Type}. Each type must be unique.");
            }

            newItems.Add(item);
        }

        Items = newItems;
    }
}
