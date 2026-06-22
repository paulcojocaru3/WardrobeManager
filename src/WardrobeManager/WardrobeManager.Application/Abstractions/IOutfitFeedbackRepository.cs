using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Abstractions;

public interface IOutfitFeedbackRepository
{
    Task AddImpressionsAsync(IEnumerable<OutfitFeedback> impressions, CancellationToken ct = default);
    Task RecordActionAsync(Guid userId, Guid generationId, Guid clothingItemId, FeedbackAction action, CancellationToken ct = default);

    // batch-mark several items of one generation with the same action (e.g. Worn/Favorited)
    Task RecordActionsForItemsAsync(Guid userId, Guid generationId, IEnumerable<Guid> clothingItemIds, FeedbackAction action, CancellationToken ct = default);

    // all impression rows for a generation (carry Rank + Action), input to the behaviour learners
    Task<IReadOnlyList<OutfitFeedback>> GetByGenerationAsync(Guid userId, Guid generationId, CancellationToken ct = default);

    Task<IReadOnlyCollection<Guid>> GetRejectedItemIdsSinceAsync(Guid userId, DateTime since, CancellationToken ct = default);

    // items surfaced (shown) since the given time, optionally limited to one slot. Drives cross-generation
    Task<IReadOnlyCollection<Guid>> GetRecentlyShownItemIdsAsync(Guid userId, DateTime since, ClothingType? slot = null, CancellationToken ct = default);
}
