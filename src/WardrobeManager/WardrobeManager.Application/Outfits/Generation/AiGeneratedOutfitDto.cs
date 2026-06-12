using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Generation;

public sealed record SimilarItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ProcessedImageUrl { get; init; }
    public double SimilarityScore { get; init; }
}

public sealed record OutfitRecommendationDto
{
    public ClothingType Type { get; init; }
    public IReadOnlyList<SimilarItemDto> TopCandidates { get; init; } = [];
}

public sealed record AiGeneratedOutfitDto
{
    // identifies this run so the client can attach feedback to its recommendations
    public Guid GenerationId { get; init; } = Guid.NewGuid();

    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<SimilarItemDto> SelectedItems { get; init; } = [];
    public IReadOnlyList<OutfitRecommendationDto> RecommendationsPerType { get; init; } = [];
    public bool IsValid { get; init; } = true; // false if any selected item fell below threshold

    // human-readable notes when a requested constraint (color/sub-type) couldn't be satisfied and the
    // generator fell back to the closest available piece
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
