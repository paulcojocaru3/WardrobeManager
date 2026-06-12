using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Scoring;

[Trait("Category", "Unit")]
public sealed class ColorHarmonyEvaluatorTests
{
    private readonly ColorHarmonyEvaluator _sut = new();

    private static OutfitGenerationContext Context(params ClothingItem[] selected)
        => new() { SelectedItems = selected.ToList() };

    [Fact]
    public void Metadata_IsStable()
    {
        Assert.Equal("ColorHarmony", _sut.Name);
        Assert.Equal(0.20, _sut.Weight);
    }

    [Fact]
    public void Evaluate_Abstains_WhenCandidateHasNoColor()
    {
        var result = _sut.Evaluate(TestData.Item(color: null), Context());
        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_NeutralCandidate_GetsPairsWithAnythingBonus()
    {
        // base 0.5 + neutral 0.2 = 0.7
        var result = _sut.Evaluate(TestData.Item(color: "white"), Context());
        Assert.Equal(0.7, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_FirstAccentColor_GetsSmallBonus()
    {
        // base 0.5 + new-accent (<=2) 0.1 = 0.6
        var result = _sut.Evaluate(TestData.Item(color: "red"), Context());
        Assert.Equal(0.6, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_ExactTopBottomColorMatch_GetsSetBonus()
    {
        var top = TestData.Item(ClothingType.Top, color: "olive");
        // base 0.5 + bottom-set-bonus 0.4 + same-family 0.15 = 1.05 -> clamped to 1.0
        var candidate = TestData.Item(ClothingType.Bottom, color: "olive");
        var result = _sut.Evaluate(candidate, Context(top));
        Assert.Equal(1.0, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_ThirdStrongAccent_IsPenalized()
    {
        var red = TestData.Item(color: "red");
        var blue = TestData.Item(color: "blue");
        // accents {red, blue} + new green -> 3 accents -> -0.3; base 0.5 -> 0.2
        var candidate = TestData.Item(color: "green");
        var result = _sut.Evaluate(candidate, Context(red, blue));
        Assert.Equal(0.2, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_FourthStrongAccent_HitsClownPenalty()
    {
        var red = TestData.Item(color: "red");
        var blue = TestData.Item(color: "blue");
        var green = TestData.Item(color: "green");
        // accents {red, blue, green} + new yellow -> 4 accents -> -0.6; base 0.5 -> -0.1
        var candidate = TestData.Item(color: "yellow");
        var result = _sut.Evaluate(candidate, Context(red, blue, green));
        Assert.Equal(-0.1, result!.Value, 3);
    }
}
