using WardrobeManager.Application.Outfits.Scoring;

namespace WardrobeManager.Tests.Unit.Scoring;

[Trait("Category", "Unit")]
public sealed class WearRotationEvaluatorTests
{
    private readonly WearRotationEvaluator _sut = new();

    [Fact]
    public void Metadata_IsStable()
    {
        Assert.Equal("WearRotation", _sut.Name);
    }

    [Fact]
    public void Evaluate_ReturnsNeutral_WhenNoRotationSignal()
    {
        Assert.Equal(1.0, _sut.Evaluate(TestData.Item(isFavorite: false), new OutfitGenerationContext()));
    }

    [Fact]
    public void Evaluate_FavoriteItem_IsBoosted()
    {
        Assert.Equal(ToMultiplier(0.6), _sut.Evaluate(TestData.Item(isFavorite: true), new OutfitGenerationContext()), 3);
    }

    [Fact]
    public void Evaluate_RediscoverMode_StronglyBoostsNeverWornItem()
    {
        var context = new OutfitGenerationContext
        {
            PreferUnusedItems = true,
            WearRecency = new Dictionary<Guid, DateTime> { [Guid.NewGuid()] = DateTime.UtcNow },
        };

        Assert.Equal(1.5, _sut.Evaluate(TestData.Item(), context), 3);
    }

    [Theory]
    [InlineData(2, -0.85)]
    [InlineData(15, -0.3)]
    [InlineData(60, 0.3)]
    [InlineData(120, 0.7)]
    [InlineData(300, 1.0)]
    public void Evaluate_RediscoverMode_UsesRecencyBuckets(double daysAgo, double expectedScore)
    {
        var item = TestData.Item();
        var context = new OutfitGenerationContext
        {
            PreferUnusedItems = true,
            WearRecency = new Dictionary<Guid, DateTime> { [item.Id] = DateTime.UtcNow.AddDays(-daysAgo) },
        };

        Assert.Equal(ToMultiplier(expectedScore), _sut.Evaluate(item, context), 3);
    }

    [Fact]
    public void Evaluate_BalancesRecentRecommendationAndUsage()
    {
        var item = TestData.Item();
        var context = new OutfitGenerationContext
        {
            MedianWearCount = 4,
            WearCounts = new Dictionary<Guid, int> { [item.Id] = 10 },
            RecentlyRecommendedItemIds = new HashSet<Guid> { item.Id },
        };

        Assert.Equal(ToMultiplier(-0.4), _sut.Evaluate(item, context), 3);
    }

    private static double ToMultiplier(double score) =>
        Math.Max(0.05, 0.05 + ((score + 1.0) / 2.0) * 1.45);
}
