using WardrobeManager.Application.Outfits.Scoring;

namespace WardrobeManager.Tests.Unit.Scoring;

[Trait("Category", "Unit")]
public sealed class ColorPreferenceEvaluatorTests
{
    private readonly ColorPreferenceEvaluator _sut = new();

    private static OutfitGenerationContext Context(
        IReadOnlyList<string>? desired = null,
        IReadOnlyList<string>? avoid = null,
        IReadOnlyList<string>? preferred = null)
        => new()
        {
            DesiredColors = desired ?? new List<string>(),
            AvoidColors = avoid ?? new List<string>(),
            PreferredColors = preferred ?? new List<string>(),
        };

    [Fact]
    public void Metadata_IsStable()
    {
        Assert.Equal("ColorPreference", _sut.Name);
    }

    [Fact]
    public void Evaluate_Abstains_WhenNoColorSignal()
    {
        Assert.Equal(1.0, _sut.Evaluate(TestData.Item(color: "red"), Context()), 3);
    }

    [Fact]
    public void Evaluate_Abstains_WhenCandidateHasNoColor()
    {
        Assert.Equal(1.0, _sut.Evaluate(TestData.Item(color: null), Context(desired: new[] { "red" })), 3);
    }

    [Fact]
    public void Evaluate_AvoidedColor_IsStronglyPenalized_VetoMovedToFeasibility()
    {
        // the hard avoid-color veto now lives in IGarmentFeasibility; the soft evaluator keeps a strong
        var result = _sut.Evaluate(TestData.Item(color: "red"), Context(avoid: new[] { "red" }));
        Assert.Equal(0.2, result, 3);
    }

    [Fact]
    public void Evaluate_DesiredColorMatch_ScoresMax()
    {
        var result = _sut.Evaluate(TestData.Item(color: "navy blue"), Context(desired: new[] { "blue" }));
        Assert.Equal(1.2, result, 3);
    }

    [Fact]
    public void Evaluate_DesiredColorMiss_IsPenalized()
    {
        var result = _sut.Evaluate(TestData.Item(color: "red"), Context(desired: new[] { "blue" }));
        Assert.Equal(0.8, result, 3);
    }

    [Fact]
    public void Evaluate_PreferredColorMatch_GetsSoftNudge()
    {
        var result = _sut.Evaluate(TestData.Item(color: "green"), Context(preferred: new[] { "green" }));
        Assert.Equal(1.1, result, 3);
    }

    [Fact]
    public void Evaluate_PreferredOnly_NoMatch_Abstains()
    {
        Assert.Equal(1.0, _sut.Evaluate(TestData.Item(color: "red"), Context(preferred: new[] { "green" })), 3);
    }
}
