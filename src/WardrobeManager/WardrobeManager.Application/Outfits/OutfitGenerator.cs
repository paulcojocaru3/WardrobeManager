using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits;

public class OutfitGenerator : IOutfitGenerator
{
    private readonly IClothingRepository _clothingRepository;
    private readonly IEnumerable<IOutfitEvaluator> _evaluators;

    public OutfitGenerator(IClothingRepository clothingRepository)
    {
        _clothingRepository = clothingRepository;
        _evaluators = new List<IOutfitEvaluator>
        {
            new WeatherEvaluator(),
            new StyleEvaluator(),
            new ColorHarmonyEvaluator(),
            new ColorPreferenceEvaluator()
        };
    }

    public async Task<AiGeneratedOutfitDto> GenerateAiOutfitAsync(Guid userId, Guid startItemId, double threshold = 0.5, WeatherData? weatherData = null, string? style = null, IReadOnlyList<string>? desiredColors = null, IReadOnlyList<string>? avoidColors = null, string? occasion = null, CancellationToken ct = default)
    {
        var startItem = await _clothingRepository.GetByIdAsync(startItemId, ct);
        
        if (startItem == null) throw new KeyNotFoundException("Start item not found.");
        if (startItem.Embedding == null) throw new InvalidOperationException("Start item has no embedding vector.");

        var result = new AiGeneratedOutfitDto
        {
            Name = $"{(style ?? "Custom")} Look with {startItem.Name}",
            SelectedItems = new List<SimilarItemDto> 
            { 
                new() { Id = startItem.Id, Name = startItem.Name, ProcessedImageUrl = startItem.ProcessedImageUrl, SimilarityScore = 1.0 } 
            },
            IsValid = true
        };

        var context = new OutfitGenerationContext
        {
            Weather = weatherData,
            TargetStyle = style,
            DesiredColors = desiredColors ?? new List<string>(),
            AvoidColors = avoidColors ?? new List<string>(),
            Occasion = occasion,
            SelectedItems = { startItem }
        };

        var neededTypes = GetNeededTypesBasedOnContext(startItem.Type, context);

        foreach (var type in neededTypes)
        {
            var similarItems = await _clothingRepository.GetSimilarItemsAsync(userId, startItem.Embedding, type: type, limit: 30, threshold: null, ct);
            var filteredCandidates = new List<SimilarItemDto>();

            foreach (var candidateTuple in similarItems)
            {
                var candidate = candidateTuple.Item;
                if (candidate.Id == startItemId) continue;

                double score = CalculateContextualScore(candidate, candidateTuple.Similarity, context);

                // If score is < 0 (Vetoed or highly penalized), discard it
                if (score >= 0)
                {
                    filteredCandidates.Add(new SimilarItemDto
                    {
                        Id = candidate.Id,
                        Name = candidate.Name,
                        ProcessedImageUrl = candidate.ProcessedImageUrl,
                        SimilarityScore = score
                    });
                }
            }

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
                
                // Add the actual item to the context for the next iteration to evaluate
                var bestItem = similarItems.First(x => x.Item.Id == bestCandidate.Id).Item;
                context.SelectedItems.Add(bestItem);

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

    private double CalculateContextualScore(ClothingItem item, double mlSimilarity, OutfitGenerationContext context)
    {
        double finalScore = 0.0;
        double totalWeight = 0.0;
        double mlWeight = 0.15; // 15% weight for Vector Similarity

        // Base Vector Similarity (normalize to [0, 1])
        finalScore += mlSimilarity * mlWeight;
        totalWeight += mlWeight;

        foreach (var evaluator in _evaluators)
        {
            double evalScore = evaluator.Evaluate(item, context);
            
            // Hard Veto: If any evaluator completely rejects the item, it's out
            if (evalScore <= -0.99)
            {
                return -1.0; 
            }

            // Normalization: evaluators return [-1, 1]. Shift to [0, 1] for weighted sum
            double normalizedEvalScore = (evalScore + 1.0) / 2.0;

            finalScore += normalizedEvalScore * evaluator.Weight;
            totalWeight += evaluator.Weight;
        }

        // Return final score clamped [0, 1]
        if (totalWeight > 0)
        {
            finalScore /= totalWeight;
        }

        return finalScore;
    }

    private List<ClothingType> GetNeededTypesBasedOnContext(ClothingType startType, OutfitGenerationContext context)
    {
        var needed = new List<ClothingType>();

        // Default basic outfit
        if (startType != ClothingType.Top) needed.Add(ClothingType.Top);
        if (startType != ClothingType.Bottom) needed.Add(ClothingType.Bottom);
        if (startType != ClothingType.Shoes) needed.Add(ClothingType.Shoes);

        // Weather based dynamics
        if (context.Weather != null)
        {
            if (context.Weather.Temperature <= 23 && startType != ClothingType.Outerwear)
            {
                needed.Add(ClothingType.Outerwear); // Need a jacket/coat if it's not hot
            }
        }
        else
        {
            // If no weather data, fallback to including Outerwear to be safe
            if (startType != ClothingType.Outerwear) needed.Add(ClothingType.Outerwear);
        }

        // Always add Accessory as an option at the end
        if (startType != ClothingType.Accessory) needed.Add(ClothingType.Accessory);

        return needed;
    }
}
