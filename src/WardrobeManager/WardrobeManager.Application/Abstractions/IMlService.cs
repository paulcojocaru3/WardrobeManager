using WardrobeManager.Application.Outfits.Learning;

namespace WardrobeManager.Application.Abstractions;

// what the ML pipeline returns for an uploaded clothing image
public record MlClothingResult(
    string? Type,
    string? Color,
    string? ProcessedImageB64,
    float[]? Embedding,
    string? Gender,
    string? Season,
    string? Usage);

public interface IMlService
{
    Task<MlClothingResult> ProcessClothingImageAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
    Task<(string Style, double Confidence, string? City)> ParsePromptAsync(string prompt, CancellationToken ct = default);

    // text embedding lands in the same CLIP space as the image embeddings
    Task<float[]> EmbedTextAsync(string text, CancellationToken ct = default);
    Task<IReadOnlyList<string>> PredictArticleTypesAsync(IReadOnlyList<float[]> embeddings, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetArticleTypesAsync(CancellationToken ct = default);

    // returns null when there isn't enough feedback to train
    Task<LearnedWeights?> TrainWeightsAsync(IReadOnlyList<WeightTrainingSample> samples, IReadOnlyList<string> featureNames, IReadOnlyDictionary<string, double> defaultWeights, CancellationToken ct = default);
}