using WardrobeManager.Application.Outfits.Scoring;

namespace WardrobeManager.Tests.Unit.Scoring;

[Trait("Category", "Unit")]
public sealed class VarietyEvaluatorTests
{
    private readonly VarietyEvaluator _sut = new();

    private static OutfitGenerationContext WithRecency(Guid id, double daysAgo)
        => new()
        {
            WearRecency = new Dictionary<Guid, DateTime> { [id] = DateTime.UtcNow.AddDays(-daysAgo) },
        };

    [Fact]
    public void Metadata_IsStable()
    {
        Assert.Equal("Variety", _sut.Name);
        Assert.Equal(0.10, _sut.Weight);
    }

    [Fact]
    public void Evaluate_Abstains_WhenNoSignal()
    {
        Assert.Null(_sut.Evaluate(TestData.Item(isFavorite: false), new OutfitGenerationContext()));
    }

    [Fact]
    public void Evaluate_FavoriteOnly_GetsFavoriteScore()
    {
        var result = _sut.Evaluate(TestData.Item(isFavorite: true), new OutfitGenerationContext());
        Assert.Equal(0.6, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_NeverWorn_GetsRediscoverScore()
    {
        // recency dict non-empty but candidate absent -> 0.8
        var context = new OutfitGenerationContext
        {
            WearRecency = new Dictionary<Guid, DateTime> { [Guid.NewGuid()] = DateTime.UtcNow },
        };
        var result = _sut.Evaluate(TestData.Item(), context);
        Assert.Equal(0.8, result!.Value, 3);
    }

    [Theory]
    [InlineData(0, -0.5)]    // worn today
    [InlineData(4, -0.2)]    // within a week
    [InlineData(10, 0.2)]    // within three weeks
    [InlineData(30, 0.6)]    // within ~six weeks
    [InlineData(60, 1.0)]    // long unworn -> bring it back
    public void Evaluate_RecencyBuckets(double daysAgo, double expected)
    {
        var item = TestData.Item();
        var result = _sut.Evaluate(item, WithRecency(item.Id, daysAgo));
        Assert.Equal(expected, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_FavoriteAndRecency_AreBlended()
    {
        // favorite 0.6 + never-worn recency 0.8 -> 0.5*0.6 + 0.5*0.8 = 0.7
        var context = new OutfitGenerationContext
        {
            WearRecency = new Dictionary<Guid, DateTime> { [Guid.NewGuid()] = DateTime.UtcNow },
        };
        var result = _sut.Evaluate(TestData.Item(isFavorite: true), context);
        Assert.Equal(0.7, result!.Value, 3);
    }
}
