using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Clothing;

public record ClothingItemDto(
    Guid Id,
    string Name,
    ClothingType Type,
    string? Color,
    string ProcessedImageUrl,
    DateTime CreatedAt
);
