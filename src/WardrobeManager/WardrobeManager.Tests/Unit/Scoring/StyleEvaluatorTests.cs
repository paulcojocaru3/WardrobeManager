using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Tests.Unit.Scoring;

[Trait("Category", "Unit")]
public sealed class StyleEvaluatorTests
{
    private readonly StyleEvaluator _sut = new();

    private static OutfitGenerationContext Context(string? style = null, int? formality = null)
        => new() { TargetStyle = style, Formality = formality };

    [Fact]
    public void Metadata_IsStable()
    {
        Assert.Equal("Style", _sut.Name);
    }

    [Fact]
    public void Evaluate_Abstains_WhenNoStyleAndNoFormality()
    {
        var result = _sut.Evaluate(TestData.Item(usage: "Casual"), Context());
        Assert.Equal(1.0, result, 3);
    }

    [Fact]
    public void Evaluate_HardMismatch_IsPenalized_VetoMovedToFeasibility()
    {
        // the hard "Sports in a Formal outfit" veto now lives in IGarmentFeasibility; the soft evaluator
        var result = _sut.Evaluate(TestData.Item(usage: "Sports"), Context(style: "Formal"));
        Assert.Equal(0.05, result, 3);
    }

    [Fact]
    public void Evaluate_ExactStyleMatch_ScoresMax()
    {
        var result = _sut.Evaluate(TestData.Item(usage: "Casual"), Context(style: "Casual"));
        Assert.Equal(1.5, result, 3);
    }

    [Fact]
    public void Evaluate_UnknownUsage_GivesMildUncertainty()
    {
        var result = _sut.Evaluate(TestData.Item(usage: null), Context(style: "Casual"));
        Assert.Equal(0.775, result, 3);
    }

    [Fact]
    public void Evaluate_AdjacentStyle_GetsSmallPenaltyOnly()
    {
        // target Formal (rank 4), usage Party (rank 3) -> distance 1 -> 0.6 (adjacent, fine)
        var result = _sut.Evaluate(TestData.Item(usage: "Party"), Context(style: "Formal"));
        Assert.Equal(0.485, result, 3);
    }

    [Fact]
    public void Evaluate_FarStyle_IsPenalized()
    {
        // target Casual (rank 1), usage Formal (rank 4) -> distance 3 -> -0.3
        var result = _sut.Evaluate(TestData.Item(usage: "Formal"), Context(style: "Casual"));
        Assert.Equal(0.05, result, 3);
    }

    [Fact]
    public void Evaluate_FormalityOnly_ScoresByDistance()
    {
        // no style; formality 3 -> desiredRank 2; usage Casual rank 1 -> diff 1 -> 0.5
        var result = _sut.Evaluate(TestData.Item(usage: "Casual"), Context(formality: 3));
        Assert.Equal(0.63, result, 3);
    }

    [Fact]
    public void Evaluate_StyleAndFormality_AreBlended()
    {
        var result = _sut.Evaluate(TestData.Item(usage: "Casual"), Context(style: "Casual", formality: 1));
        Assert.Equal(1.239, result, 3);
    }
}
