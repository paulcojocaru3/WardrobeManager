using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Queries;

public class OutfitRecommendationDto
{
    public ClothingType Type { get; set; }
    public List<SimilarItemDto> TopCandidates { get; set; } = new();
}

public class SimilarItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProcessedImageUrl { get; set; }
    public double SimilarityScore { get; set; }
}

public class AiGeneratedOutfitDto
{
    public string Name { get; set; } = string.Empty;
    public List<SimilarItemDto> SelectedItems { get; set; } = new();
    public List<OutfitRecommendationDto> RecommendationsPerType { get; set; } = new();
    public bool IsValid { get; set; } // If all selected items pass threshold
}
