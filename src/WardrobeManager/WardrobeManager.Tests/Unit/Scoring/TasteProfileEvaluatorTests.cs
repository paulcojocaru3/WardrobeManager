using WardrobeManager.Application.Outfits.Scoring;

namespace WardrobeManager.Tests.Unit.Scoring;

[Trait("Category", "Unit")]
public sealed class TasteProfileEvaluatorTests
{
    private readonly TasteProfileEvaluator _sut = new();

    [Fact]
    public void Metadata_IsStable()
    {
        Assert.Equal("Taste", _sut.Name);
    }

    [Fact]
    public void Evaluate_Abstains_WhenNoProfile()
    {
        Assert.Equal(1.0, _sut.Evaluate(TestData.Item(color: "blue", usage: "Casual"), new OutfitGenerationContext()), 3);
    }

    [Fact]
    public void Evaluate_Abstains_WhenItemColorAndStyleUnknownToProfile()
    {
        var context = new OutfitGenerationContext
        {
            LearnedColorScores = new Dictionary<string, double> { ["red"] = 0.9 },
            LearnedStyleScores = new Dictionary<string, double> { ["formal"] = 0.9 },
        };
        Assert.Equal(1.0, _sut.Evaluate(TestData.Item(color: "blue", usage: "Casual"), context), 3);
    }

    [Fact]
    public void Evaluate_FavoriteColor_GivesPositiveSignal()
    {
        var context = new OutfitGenerationContext
        {
            LearnedColorScores = new Dictionary<string, double> { ["blue"] = 1.0 },
        };
        // navy normalizes to the "blue" key; score 1.0 -> signal (1.0 - 0.5) * 2 = +1.0
        Assert.Equal(1.15, _sut.Evaluate(TestData.Item(color: "navy"), context), 3);
    }

    [Fact]
    public void Evaluate_DislikedColor_GivesNegativeSignal_ButNeverVetoes()
    {
        var context = new OutfitGenerationContext
        {
            LearnedColorScores = new Dictionary<string, double> { ["blue"] = 0.0 },
        };
        Assert.Equal(0.85, _sut.Evaluate(TestData.Item(color: "blue"), context), 3);
    }

    [Fact]
    public void Evaluate_AveragesColorAndStyle()
    {
        var context = new OutfitGenerationContext
        {
            LearnedColorScores = new Dictionary<string, double> { ["blue"] = 1.0 },
            LearnedStyleScores = new Dictionary<string, double> { ["casual"] = 0.5 },
        };
        // avg(1.0, 0.5) = 0.75 -> signal (0.75 - 0.5) * 2 = 0.5
        Assert.Equal(1.075, _sut.Evaluate(TestData.Item(color: "blue", usage: "Casual"), context), 3);
    }
}
