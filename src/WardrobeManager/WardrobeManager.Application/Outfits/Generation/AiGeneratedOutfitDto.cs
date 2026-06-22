using WardrobeManager.Application.Abstractions;
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
    public Guid GenerationId { get; init; } = Guid.NewGuid();

    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<SimilarItemDto> SelectedItems { get; init; } = [];
    public IReadOnlyList<OutfitRecommendationDto> RecommendationsPerType { get; init; } = [];
    public bool IsValid { get; init; } = true;

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public IReadOnlyList<OutfitCandidate> Candidates { get; init; } = [];

    // mark direct gemma3 generation.
    public bool GeneratedByStylist { get; init; }
    public string? StylistHeadline { get; init; }
    public IReadOnlyList<string> StylistHighlights { get; init; } = [];
    public string? StylistTip { get; init; }
}
