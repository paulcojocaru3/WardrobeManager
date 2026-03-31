using WardrobeManager.Application.Clothing.Queries;

namespace WardrobeManager.Application.Outfits.Queries;

public record OutfitDto(
    Guid Id,
    string Name,
    bool IsAiGenerated,
    DateTime CreatedAt,
    List<ClothingItemDto> Items
);
