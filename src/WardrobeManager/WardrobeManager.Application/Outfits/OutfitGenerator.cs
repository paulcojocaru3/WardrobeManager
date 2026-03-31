using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits;

public class OutfitGenerator(IClothingRepository clothingRepository) : IOutfitGenerator
{
    private const double WeatherBoost = 0.2;
    private const double StyleBoost = 0.3;
    private const double RainBoost = 0.1;

    public async Task<AiGeneratedOutfitDto> GenerateAiOutfitAsync(Guid userId, Guid startItemId, double threshold = 0.5, WeatherData? weatherData = null, string? style = null, CancellationToken ct = default)
    {
        var startItem = await clothingRepository.GetByIdAsync(startItemId, ct);
        
        if (startItem == null)
        {
            throw new KeyNotFoundException("Start item not found.");
        }
        
        if (startItem.Embedding == null)
        {
            throw new InvalidOperationException("Start item has no embedding vector.");
        }

        var result = new AiGeneratedOutfitDto
        {
            Name = $"{(style ?? "Custom")} Look with {startItem.Name}",
            SelectedItems = new List<SimilarItemDto> 
            { 
                new() { Id = startItem.Id, Name = startItem.Name, ProcessedImageUrl = startItem.ProcessedImageUrl, SimilarityScore = 1.0 } 
            },
            IsValid = true
        };

        var neededTypes = GetNeededTypes(startItem.Type);

        foreach (var type in neededTypes)
        {
            // Fetch potential candidates
            var similarItems = await clothingRepository.GetSimilarItemsAsync(userId, startItem.Embedding, type: type, limit: 20, threshold: null, ct);
            
            var filteredCandidates = new List<SimilarItemDto>();

            foreach (var candidate in similarItems)
            {
                if (candidate.Item.Id == startItemId) continue;

                double score = CalculateSelectionScore(candidate.Item, candidate.Similarity, weatherData, style);

                // If score is 0, it means it's hard incompatible based on our rules
                if (score > 0)
                {
                    filteredCandidates.Add(new SimilarItemDto
                    {
                        Id = candidate.Item.Id,
                        Name = candidate.Item.Name,
                        ProcessedImageUrl = candidate.Item.ProcessedImageUrl,
                        SimilarityScore = score
                    });
                }
            }

            // Sort by the new calculated score
            var sortedCandidates = filteredCandidates.OrderByDescending(x => x.SimilarityScore).ToList();

            result.RecommendationsPerType.Add(new OutfitRecommendationDto
            {
                Type = type,
                TopCandidates = sortedCandidates.Take(3).ToList()
            });

            var bestCandidate = sortedCandidates.FirstOrDefault();
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
                result.IsValid = false;
            }
        }

        return result;
    }

    private double CalculateSelectionScore(ClothingItem item, double similarity, WeatherData? weather, string? requestedStyle)
    {
        string itemUsage = item.Usage ?? "";

        // 1. HARD EXCLUSION RULES
        if (!string.IsNullOrEmpty(requestedStyle))
        {
            if (requestedStyle.Equals("Formal", StringComparison.OrdinalIgnoreCase))
            {
                if (itemUsage.Contains("Sports", StringComparison.OrdinalIgnoreCase) || 
                    itemUsage.Contains("Travel", StringComparison.OrdinalIgnoreCase))
                {
                    return 0; // Incompatible
                }
            }

            if (requestedStyle.Equals("Sports", StringComparison.OrdinalIgnoreCase))
            {
                if (itemUsage.Contains("Formal", StringComparison.OrdinalIgnoreCase) || 
                    itemUsage.Contains("Party", StringComparison.OrdinalIgnoreCase))
                {
                    return 0; // Incompatible
                }
            }
        }

        double finalScore = similarity;

        // 2. WEATHER BOOST
        if (weather != null)
        {
            if (!string.IsNullOrEmpty(item.Season) && 
                item.Season.Contains(weather.SeasonSuggestion, StringComparison.OrdinalIgnoreCase))
            {
                finalScore += WeatherBoost;
            }

            if (weather.Condition.Contains("Rain", StringComparison.OrdinalIgnoreCase) && item.Type == ClothingType.Outerwear)
            {
                finalScore += RainBoost;
            }
        }

        // 3. STYLE MATCH BOOST
        if (!string.IsNullOrEmpty(requestedStyle))
        {
            if (itemUsage.Contains(requestedStyle, StringComparison.OrdinalIgnoreCase))
            {
                finalScore += StyleBoost;
            }
        }

        // Ensure score stays within [0, 1] range
        if (finalScore > 1.0) finalScore = 1.0;
        if (finalScore < 0.0) finalScore = 0.0;

        return finalScore;
    }

    private List<ClothingType> GetNeededTypes(ClothingType startType)
    {
        var allTypes = Enum.GetValues<ClothingType>().ToList();
        var needed = new List<ClothingType>();
        
        foreach (var t in allTypes)
        {
            if (t != startType)
            {
                needed.Add(t);
            }
        }
        
        return needed;
    }
}
