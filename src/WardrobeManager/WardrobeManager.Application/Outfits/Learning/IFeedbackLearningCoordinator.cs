namespace WardrobeManager.Application.Outfits.Learning;

// single entry point the feedback / wear / favorite flows call to update the behaviour learners
public interface IFeedbackLearningCoordinator
{
    Task LearnFromGenerationAsync(Guid userId, Guid generationId, CancellationToken ct = default);
}
