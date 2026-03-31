using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits;

public class OutfitGenerator(IClothingRepository clothingRepository) : IOutfitGenerator
{
    public async Task<AiGeneratedOutfitDto> GenerateAiOutfitAsync(Guid userId, Guid startItemId, double threshold = 0.5, CancellationToken ct = default)
    {
        var startItem = await clothingRepository.GetByIdAsync(startItemId, ct);
        if (startItem == null) throw new KeyNotFoundException("Start item not found.");
        if (startItem.Embedding == null) throw new InvalidOperationException("Start item has no embedding vector.");

        var neededTypes = GetNeededTypes(startItem.Type);
        var result = new AiGeneratedOutfitDto
        {
            Name = $"Generated Outfit with {startItem.Name}",
            SelectedItems = new List<SimilarItemDto> 
            { 
                new() { Id = startItem.Id, Name = startItem.Name, ProcessedImageUrl = startItem.ProcessedImageUrl, SimilarityScore = 1.0 } 
            },
            IsValid = true
        };

        foreach (var type in neededTypes)
        {
            // Fetch top 3 most similar items for this category from Postgres
            var similarItems = await clothingRepository.GetSimilarItemsAsync(userId, startItem.Embedding, type: type, limit: 3, threshold: null, ct);
            
            // Map candidates and exclude startItem (though type filtering usually takes care of it)
            var typeCandidates = similarItems
                .Where(x => x.Item.Id != startItemId)
                .Select(x => new SimilarItemDto
                {
                    Id = x.Item.Id,
                    Name = x.Item.Name,
                    ProcessedImageUrl = x.Item.ProcessedImageUrl,
                    SimilarityScore = x.Similarity
                })
                .ToList();

            var recommendation = new OutfitRecommendationDto
            {
                Type = type,
                TopCandidates = typeCandidates
            };
            result.RecommendationsPerType.Add(recommendation);

            // Select the best candidate if it exists
            var bestCandidate = typeCandidates.FirstOrDefault();
            if (bestCandidate != null)
            {
                result.SelectedItems.Add(bestCandidate);
                if (bestCandidate.SimilarityScore < threshold)
                {
                    result.IsValid = false;
                }
            }
            else
            {
                // No item found for this type
                result.IsValid = false;
            }
        }

        return result;
    }

    private List<ClothingType> GetNeededTypes(ClothingType startType)
    {
        var allTypes = Enum.GetValues<ClothingType>().ToList();
        allTypes.Remove(startType);
        // Exclude Underwear from standard outfit generation unless it's explicitly needed?
        // Let's keep it simple and include all other types for now.
        return allTypes;
    }
}
