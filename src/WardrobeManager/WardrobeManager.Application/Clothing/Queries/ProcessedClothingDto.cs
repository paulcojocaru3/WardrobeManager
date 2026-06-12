using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Clothing.Queries;

public record ProcessedClothingDto(
    string Name,
    ClothingType Type,
    string? SubType,
    string? Color,
    string? Gender,
    string? Season,
    string? Usage,
    string ProcessedImageB64,
    float[]? Embedding
);