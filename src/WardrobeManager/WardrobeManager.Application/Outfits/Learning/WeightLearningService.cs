using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Learning;

public sealed class WeightLearningService(
    IOutfitFeedbackRepository feedbackRepository,
    IUserEvaluatorWeightsRepository weightsRepository,
    IMlService mlService) : IWeightLearningService
{
    // "MlSimilarity" is treated as just another feature so the model can learn its importance too.
    private static readonly string[] FeatureNames =
        { "MlSimilarity", "Weather", "Style", "ColorHarmony", "ColorPreference", "Variety" };

    private static readonly Dictionary<string, double> DefaultWeights = new()
    {
        ["MlSimilarity"] = 0.15,
        ["Weather"] = 0.40,
        ["Style"] = 0.30,
        ["ColorHarmony"] = 0.20,
        ["ColorPreference"] = 0.20,
        ["Variety"] = 0.10
    };

    private const int MinSamples = 12; // below this the fit is too noisy to trust

    public async Task RetrainAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await feedbackRepository.GetTrainingRowsAsync(userId, ct);
        var samples = rows
            .Select(r => new WeightTrainingSample(new Dictionary<string, double>(r.EvaluatorScores), IsPositive(r.Action) ? 1 : 0))
            .ToList();

        if (samples.Count < MinSamples) return;
        if (samples.All(s => s.Label == samples[0].Label)) return; // need both classes

        var learned = await mlService.TrainWeightsAsync(samples, FeatureNames, DefaultWeights, ct);
        if (learned?.Weights is not { Count: > 0 }) return;

        double mlWeight = learned.Weights.GetValueOrDefault("MlSimilarity", DefaultWeights["MlSimilarity"]);
        var evaluatorWeights = learned.Weights
            .Where(kv => kv.Key != "MlSimilarity")
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        await weightsRepository.UpsertAsync(new UserEvaluatorWeights
        {
            UserId = userId,
            Weights = evaluatorWeights,
            MlWeight = mlWeight,
            TrainedOnSamples = samples.Count,
            UpdatedAt = DateTime.UtcNow
        }, ct);
    }

    private static bool IsPositive(FeedbackAction action) =>
        action is FeedbackAction.Accepted or FeedbackAction.Worn or FeedbackAction.Favorited;
}
