using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Domain.Entities;

public class ClothingItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public WardrobeManager.Domain.Enums.ClothingType Type { get; set; }
    // fine-grained ML article type (e.g. "shorts", "jeans", "tshirts"); enables garment-specific requests.
    public string? SubType { get; set; }
    public string? Color { get; set; }
    // second dominant/accent colour, so multi-colour pieces aren't reduced to a single tone.
    public string? SecondaryColor { get; set; }
    public string? Material { get; set; }
    // visual pattern: solid, striped, plaid, floral, graphic, ... (drives pattern-clash scoring).
    public string? Pattern { get; set; }
    // 1 (gym/loungewear) .. 5 (black-tie). Finer formality signal than the Usage label.
    public int? Formality { get; set; }
    public string? Gender { get; set; }
    public string? Season { get; set; }
    public string? Usage { get; set; }
    public string? ProcessedImageUrl { get; set; }
    public bool IsFavorite { get; set; }
    public float[]? Embedding { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // navigation properties
    public User? User { get; set; }
    public List<Outfit> Outfits { get; set; } = new();
    public List<WearEvent> WearEvents { get; set; } = new();

    public static ClothingItem Create(
        Guid userId,
        string name,
        ClothingType type,
        string? subType,
        string? color,
        string? gender,
        string? season,
        string? usage,
        string processedImage,
        float[]? embedding,
        DateTime createdAt)
    {
        return new ClothingItem
        {
            UserId = userId,
            Name = name,
            Type = type,
            SubType = subType,
            Color = color,
            Gender = gender,
            Season = season,
            Usage = usage,
            ProcessedImageUrl = NormalizeProcessedImage(processedImage),
            Embedding = embedding,
            CreatedAt = createdAt
        };
    }

    public void UpdateDetails(
        string name,
        ClothingType type,
        string? subType,
        string? color,
        string? gender,
        string? season,
        string? usage)
    {
        Name = name;
        Type = type;
        SubType = string.IsNullOrWhiteSpace(subType) ? null : subType.Trim().ToLowerInvariant();
        Color = color;
        Gender = gender;
        Season = season;
        Usage = usage;
    }

    private static string NormalizeProcessedImage(string processedImage)
    {
        return processedImage.StartsWith("data:image", StringComparison.OrdinalIgnoreCase)
            ? processedImage
            : $"data:image/png;base64,{processedImage}";
    }
}
