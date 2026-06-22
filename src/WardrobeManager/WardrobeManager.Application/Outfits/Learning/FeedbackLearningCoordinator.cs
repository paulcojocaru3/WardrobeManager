using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Learning;

public sealed class FeedbackLearningCoordinator(
    IOutfitFeedbackRepository feedbackRepository,
    IClothingRepository clothingRepository,
    ItemPairLearningService pairLearning,
    UserLearningProfileService profileLearning,
    ILogger<FeedbackLearningCoordinator> logger) : IFeedbackLearningCoordinator
{
    public async Task LearnFromGenerationAsync(Guid userId, Guid generationId, CancellationToken ct = default)
    {
        var rows = await feedbackRepository.GetByGenerationAsync(userId, generationId, ct);
        var actionedRows = rows.Where(r => r.Action != FeedbackAction.Shown).ToList();
        if (actionedRows.Count == 0) return;

        var occasion = rows.Select(r => r.Occasion).FirstOrDefault(o => !string.IsNullOrEmpty(o));

        // resolve the items once and share them with both learners.
        var itemIds = actionedRows.Select(r => r.ClothingItemId).Distinct().ToList();
        var items = await clothingRepository.GetByIdsAsync(itemIds, ct);
        var byId = items.ToDictionary(i => i.Id);

        var actioned = actionedRows
            .Where(r => byId.ContainsKey(r.ClothingItemId))
            .Select(r => new ActionedItem(byId[r.ClothingItemId], r.Action, r.Rank))
            .ToList();
        if (actioned.Count == 0) return;

        await pairLearning.AccrueAsync(userId, actioned, ct);
        await profileLearning.UpdateAsync(userId, occasion, actioned, ct);

        logger.LogInformation("Behaviour learning ran for user {UserId}, generation {GenerationId} ({Count} items).",
            userId, generationId, actioned.Count);
    }
}
