using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Learning;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Learning;

[Trait("Category", "Unit")]
public sealed class ItemPairLearningServiceTests
{
    private readonly IItemPairScoreRepository _repo = Substitute.For<IItemPairScoreRepository>();
    private ItemPairLearningService Sut() => new(_repo, NullLogger<ItemPairLearningService>.Instance);

    private static ActionedItem Row(FeedbackAction action, int rank = 0)
        => new(TestData.Item(), action, rank);

    private async Task<IReadOnlyCollection<ItemPairDelta>?> Capture(params ActionedItem[] items)
    {
        IReadOnlyCollection<ItemPairDelta>? captured = null;
        _repo.UpsertBatchAsync(Arg.Any<Guid>(), Arg.Do<IReadOnlyCollection<ItemPairDelta>>(d => captured = d))
            .Returns(Task.CompletedTask);
        await Sut().AccrueAsync(Guid.NewGuid(), items);
        return captured;
    }

    private static (int Paired, int Positive, int Negative) Sum(IReadOnlyCollection<ItemPairDelta> d)
        => (d.Sum(x => x.PairedDelta), d.Sum(x => x.PositiveDelta), d.Sum(x => x.NegativeDelta));

    [Fact]
    public async Task Accrue_PositivePairs_AmongAcceptedItems()
    {
        // 3 accepted items -> 3 unordered pairs, all positive.
        var captured = await Capture(Row(FeedbackAction.Accepted), Row(FeedbackAction.Worn), Row(FeedbackAction.Favorited));

        Assert.NotNull(captured);
        var (paired, positive, negative) = Sum(captured!);
        Assert.Equal(3, paired);
        Assert.Equal(3, positive);
        Assert.Equal(0, negative);
    }

    [Fact]
    public async Task Accrue_NegativePairs_FromActiveSwapOut()
    {
        // 2 accepted + 1 rank-0 rejected (swap-out) -> 1 positive pair, 2 negative pairs.
        var captured = await Capture(
            Row(FeedbackAction.Accepted), Row(FeedbackAction.Accepted),
            Row(FeedbackAction.Rejected, rank: 0));

        Assert.NotNull(captured);
        var (paired, positive, negative) = Sum(captured!);
        Assert.Equal(3, paired);
        Assert.Equal(1, positive);
        Assert.Equal(2, negative);
    }

    [Fact]
    public async Task Accrue_IgnoresLowerRankedRejections()
    {
        // 1 accepted + 1 rank>0 rejected -> nothing to learn (no positive pair, rejection ignored).
        IReadOnlyCollection<ItemPairDelta>? captured = null;
        _repo.UpsertBatchAsync(Arg.Any<Guid>(), Arg.Do<IReadOnlyCollection<ItemPairDelta>>(d => captured = d))
            .Returns(Task.CompletedTask);

        await Sut().AccrueAsync(Guid.NewGuid(), new[] { Row(FeedbackAction.Accepted), Row(FeedbackAction.Rejected, rank: 2) });

        Assert.Null(captured);
        await _repo.DidNotReceive().UpsertBatchAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<ItemPairDelta>>());
    }
}
