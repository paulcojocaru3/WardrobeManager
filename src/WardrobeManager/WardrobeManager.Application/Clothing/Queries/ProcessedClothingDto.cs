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
    float[]? Embedding,
    // items already in the wardrobe that look very close to this upload (informational, never blocking).
    IReadOnlyList<DuplicateCandidate> PossibleDuplicates
);

public record DuplicateCandidate(Guid Id, string Name, string ImageUrl, double Similarity);
