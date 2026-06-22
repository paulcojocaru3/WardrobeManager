using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Clothing.Queries;

public record ClothingItemDto(
    Guid Id,
    string Name,
    ClothingType Type,
    string? SubType,
    string? Color,
    string? Gender,
    string? Season,
    string? Usage,
    string ProcessedImageUrl,
    DateTime CreatedAt
)
{
    public static ClothingItemDto From(ClothingItem item) => new(
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

// a wardrobe item paired with its visual similarity (cosine, 0..1) to a query item.
public record SimilarItemDto(ClothingItemDto Item, double Similarity);
