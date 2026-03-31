using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Clothing.Queries;

public record ClothingItemDto(
    Guid Id,
    string Name,
    ClothingType Type,
    string? Color,
    string? Gender,
    string? Season,
    string? Usage,
    string ProcessedImageUrl,
    DateTime CreatedAt
);
