using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Outfits.Learning;

// learns "these two items go together for this user" from each generation's feedback:
public sealed class ItemPairLearningService(
    IItemPairScoreRepository pairScoreRepository,
    ILogger<ItemPairLearningService> logger)
{
    public async Task AccrueAsync(Guid userId, IReadOnlyList<ActionedItem> actioned, CancellationToken ct = default)
    {
        var positives = actioned
            .Where(a => FeedbackActions.IsPositive(a.Action))
            .Select(a => a.Item.Id).Distinct().ToList();

        var swapOuts = actioned
            .Where(FeedbackActions.IsActiveSwapOut)
            .Select(a => a.Item.Id).Distinct().ToList();

        var deltas = new List<ItemPairDelta>();

        // positive co-occurrence among the items the user kept together.
        for (var i = 0; i < positives.Count; i++)
            for (var j = i + 1; j < positives.Count; j++)
                deltas.Add(new ItemPairDelta(positives[i], positives[j], PairedDelta: 1, PositiveDelta: 1, NegativeDelta: 0));

        // each active swap-out paired with each kept item -> negative ("chose the other over this").
        foreach (var outId in swapOuts)
            foreach (var posId in positives)
                if (outId != posId)
                    deltas.Add(new ItemPairDelta(outId, posId, PairedDelta: 1, PositiveDelta: 0, NegativeDelta: 1));

        if (deltas.Count == 0) return;

        await pairScoreRepository.UpsertBatchAsync(userId, deltas, ct);
        logger.LogInformation("Accrued {Count} item-pair updates for user {UserId}.", deltas.Count, userId);
    }
}
