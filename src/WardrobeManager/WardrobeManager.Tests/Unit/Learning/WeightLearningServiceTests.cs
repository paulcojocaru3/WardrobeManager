using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Learning;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Learning;

[Trait("Category", "Unit")]
public sealed class WeightLearningServiceTests
{
    private readonly IOutfitFeedbackRepository _feedback = Substitute.For<IOutfitFeedbackRepository>();
    private readonly IUserEvaluatorWeightsRepository _weights = Substitute.For<IUserEvaluatorWeightsRepository>();
    private readonly IMlService _ml = Substitute.For<IMlService>();

    private WeightLearningService Sut() => new(_feedback, _weights, _ml);

    private static OutfitFeedback Row(FeedbackAction action) => new()
    {
        Action = action,
        EvaluatorScores = new Dictionary<string, double> { ["Weather"] = 0.5, ["Style"] = 0.4 },
    };

    private void GivenRows(params FeedbackAction[] actions)
        => _feedback.GetTrainingRowsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                    .Returns(actions.Select(Row).ToList());

    [Fact]
    public async Task RetrainAsync_DoesNothing_BelowMinSamples()
    {
        GivenRows(FeedbackAction.Accepted, FeedbackAction.Rejected); // only 2

        await Sut().RetrainAsync(Guid.NewGuid());

        await _ml.DidNotReceive().TrainWeightsAsync(Arg.Any<IReadOnlyList<WeightTrainingSample>>(),
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyDictionary<string, double>>(), Arg.Any<CancellationToken>());
        await _weights.DidNotReceive().UpsertAsync(Arg.Any<UserEvaluatorWeights>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetrainAsync_DoesNothing_WhenOnlyOneClassPresent()
    {
        GivenRows(Enumerable.Repeat(FeedbackAction.Accepted, 15).ToArray());

        await Sut().RetrainAsync(Guid.NewGuid());

        await _weights.DidNotReceive().UpsertAsync(Arg.Any<UserEvaluatorWeights>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetrainAsync_PersistsSplitWeights_WhenTrainingSucceeds()
    {
        var actions = Enumerable.Repeat(FeedbackAction.Accepted, 8)
            .Concat(Enumerable.Repeat(FeedbackAction.Rejected, 8)).ToArray();
        GivenRows(actions);

        var learned = new LearnedWeights(new Dictionary<string, double>
        {
            ["MlSimilarity"] = 0.25,
            ["Weather"] = 0.5,
            ["Style"] = 0.3,
            ["ColorHarmony"] = 0.2,
            ["ColorPreference"] = 0.2,
            ["Variety"] = 0.1,
        }, NSamples: 16);
        _ml.TrainWeightsAsync(Arg.Any<IReadOnlyList<WeightTrainingSample>>(), Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<IReadOnlyDictionary<string, double>>(), Arg.Any<CancellationToken>())
            .Returns(learned);

        var userId = Guid.NewGuid();
        await Sut().RetrainAsync(userId);

        await _weights.Received(1).UpsertAsync(Arg.Is<UserEvaluatorWeights>(w =>
            w.UserId == userId &&
            w.MlWeight == 0.25 &&
            !w.Weights.ContainsKey("MlSimilarity") &&
            w.Weights.Count == 5 &&
            w.TrainedOnSamples == 16), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetrainAsync_DoesNotPersist_WhenTrainerReturnsNull()
    {
        var actions = Enumerable.Repeat(FeedbackAction.Accepted, 8)
            .Concat(Enumerable.Repeat(FeedbackAction.Rejected, 8)).ToArray();
        GivenRows(actions);
        _ml.TrainWeightsAsync(Arg.Any<IReadOnlyList<WeightTrainingSample>>(), Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<IReadOnlyDictionary<string, double>>(), Arg.Any<CancellationToken>())
            .Returns((LearnedWeights?)null);

        await Sut().RetrainAsync(Guid.NewGuid());

        await _weights.DidNotReceive().UpsertAsync(Arg.Any<UserEvaluatorWeights>(), Arg.Any<CancellationToken>());
    }
}
