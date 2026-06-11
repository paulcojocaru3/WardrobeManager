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
        Assert.Equal(0.20, _sut.Weight);
    }

    [Fact]
    public void Evaluate_Abstains_WhenNoColorSignal()
    {
        Assert.Null(_sut.Evaluate(TestData.Item(color: "red"), Context()));
    }

    [Fact]
    public void Evaluate_Abstains_WhenCandidateHasNoColor()
    {
        Assert.Null(_sut.Evaluate(TestData.Item(color: null), Context(desired: new[] { "red" })));
    }

    [Fact]
    public void Evaluate_AvoidedColor_Vetoes()
    {
        var result = _sut.Evaluate(TestData.Item(color: "red"), Context(avoid: new[] { "red" }));
        Assert.Equal(-1.0, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_DesiredColorMatch_ScoresMax()
    {
        var result = _sut.Evaluate(TestData.Item(color: "navy blue"), Context(desired: new[] { "blue" }));
        Assert.Equal(1.0, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_DesiredColorMiss_IsPenalized()
    {
        var result = _sut.Evaluate(TestData.Item(color: "red"), Context(desired: new[] { "blue" }));
        Assert.Equal(-0.3, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_PreferredColorMatch_GetsSoftNudge()
    {
        var result = _sut.Evaluate(TestData.Item(color: "green"), Context(preferred: new[] { "green" }));
        Assert.Equal(0.5, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_PreferredOnly_NoMatch_Abstains()
    {
        Assert.Null(_sut.Evaluate(TestData.Item(color: "red"), Context(preferred: new[] { "green" })));
    }
}
