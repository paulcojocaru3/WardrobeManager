namespace WardrobeManager.Domain.Entities;

public class ClothingItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public WardrobeManager.Domain.Enums.ClothingType Type { get; set; }
    // Fine-grained ML article type (e.g. "shorts", "jeans", "tshirts"); enables garment-specific requests.
    public string? SubType { get; set; }
    public string? Color { get; set; }
    public string? Material { get; set; }
    public string? Gender { get; set; }
    public string? Season { get; set; }
    public string? Usage { get; set; }
    public string OriginalImageUrl { get; set; } = string.Empty;
    public string? ProcessedImageUrl { get; set; }
    public bool IsFavorite { get; set; }
    public float[]? Embedding { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public List<Outfit> Outfits { get; set; } = new();
    public List<WearEvent> WearEvents { get; set; } = new();
}
