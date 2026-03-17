using WardrobeManager.Application.Clothing;

namespace WardrobeManager.Application.Outfits;

public record OutfitDto(
    Guid Id,
    string Name,
    bool IsAiGenerated,
    DateTime CreatedAt,
    List<ClothingItemDto> Items
);
