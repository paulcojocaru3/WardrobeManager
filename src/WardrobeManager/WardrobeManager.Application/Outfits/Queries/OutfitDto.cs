using WardrobeManager.Application.Clothing.Queries;

namespace WardrobeManager.Application.Outfits.Queries;

public record OutfitDto(
    Guid Id,
    string Name,
    bool IsAiGenerated,
    bool IsFavorite,
    List<string> Tags,
    DateTime CreatedAt,
    List<ClothingItemDto> Items
);
