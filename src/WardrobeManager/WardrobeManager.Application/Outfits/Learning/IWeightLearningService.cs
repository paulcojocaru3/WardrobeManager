namespace WardrobeManager.Application.Outfits.Learning;

// refits a user's evaluator weights from their accumulated feedback
public interface IWeightLearningService
{
    Task RetrainAsync(Guid userId, CancellationToken ct = default);
}
