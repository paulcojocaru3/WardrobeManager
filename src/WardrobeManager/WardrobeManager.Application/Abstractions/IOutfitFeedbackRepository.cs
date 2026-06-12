using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Abstractions;

public interface IOutfitFeedbackRepository
{
    Task AddImpressionsAsync(IEnumerable<OutfitFeedback> impressions, CancellationToken ct = default);
    Task RecordActionAsync(Guid userId, Guid generationId, Guid clothingItemId, FeedbackAction action, CancellationToken ct = default);

    // rows that carry a training label (anything other than Shown)
    Task<IReadOnlyList<OutfitFeedback>> GetTrainingRowsAsync(Guid userId, CancellationToken ct = default);
    Task<int> CountActionableAsync(Guid userId, CancellationToken ct = default);
}
