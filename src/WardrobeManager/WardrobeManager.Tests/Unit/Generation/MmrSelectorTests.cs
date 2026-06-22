using WardrobeManager.Application.Outfits.Generation;

namespace WardrobeManager.Tests.Unit.Generation;

[Trait("Category", "Unit")]
public sealed class MmrSelectorTests
{
    private static (string, double, float[]?) C(string id, double relevance, float[]? embedding = null)
        => (id, relevance, embedding);

    [Fact]
    public void Select_ReturnsEmpty_WhenNoCandidates()
    {
        Assert.Empty(MmrSelector.Select(Array.Empty<(string, double, float[]?)>(), 3));
    }

    [Fact]
    public void Select_ReturnsEmpty_WhenCountNotPositive()
    {
        var candidates = new[] { C("a", 1.0) };
        Assert.Empty(MmrSelector.Select(candidates, 0));
    }

    [Fact]
    public void Select_OrdersByRelevance_WhenNoEmbeddings()
    {
        var candidates = new[] { C("low", 0.1), C("high", 0.9), C("mid", 0.5) };

        var result = MmrSelector.Select(candidates, 3);

        Assert.Equal(new[] { "high", "mid", "low" }, result);
    }

    [Fact]
    public void Select_LimitsToRequestedCount()
    {
        var candidates = new[]
        {
            C("a", 0.9, new[] { 1f, 0f }),
            C("b", 0.8, new[] { 0f, 1f }),
            C("c", 0.7, new[] { 1f, 1f }),
        };

        Assert.Equal(2, MmrSelector.Select(candidates, 2).Count);
    }

    [Fact]
    public void Select_SeedsWithMostRelevant_ThenPrefersDistinctItem()
    {
        // "twin" is near-identical to the top-relevance "anchor"; "distinct" is orthogonal.
        var candidates = new[]
        {
            C("anchor", 1.0, new[] { 1f, 0f }),
            C("twin", 0.95, new[] { 1f, 0.01f }),
            C("distinct", 0.6, new[] { 0f, 1f }),
        };

        var result = MmrSelector.Select(candidates, 2, lambda: 0.5);

        Assert.Equal("anchor", result[0]);
        Assert.Equal("distinct", result[1]); // diversity beats the slightly-more-relevant twin
    }

    [Fact]
    public void Select_ClampsLambdaOutOfRange()
    {
        var candidates = new[] { C("a", 0.9, new[] { 1f, 0f }), C("b", 0.5, new[] { 0f, 1f }) };

        // lambda above 1 must not throw and still yields a full selection
        Assert.Equal(2, MmrSelector.Select(candidates, 2, lambda: 5.0).Count);
    }
}
