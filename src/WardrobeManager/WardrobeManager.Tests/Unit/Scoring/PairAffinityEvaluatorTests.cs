using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Tests.Unit.Scoring;

[Trait("Category", "Unit")]
public sealed class PairAffinityEvaluatorTests
{
    private readonly PairAffinityEvaluator _sut = new();

    [Fact]
    public void Metadata_IsStable()
    {
        Assert.Equal("PairAffinity", _sut.Name);
    }

    [Fact]
    public void Evaluate_Abstains_WhenNoPairData()
    {
        var candidate = TestData.Item();
        var context = new OutfitGenerationContext { SelectedItems = { TestData.Item() } };
        Assert.Equal(1.0, _sut.Evaluate(candidate, context), 3);
    }

    [Fact]
    public void Evaluate_Abstains_WhenCandidateHasNoKnownPair()
    {
        var candidate = TestData.Item();
        var selected = TestData.Item();
        var unrelatedA = Guid.NewGuid();
        var unrelatedB = Guid.NewGuid();

        var context = new OutfitGenerationContext
        {
            SelectedItems = { selected },
            PairCompatibility = new Dictionary<(Guid, Guid), double> { [ItemPair.Canonical(unrelatedA, unrelatedB)] = 1.0 },
        };

        Assert.Equal(1.0, _sut.Evaluate(candidate, context), 3);
    }

    [Fact]
    public void Evaluate_AveragesKnownPairCompatibilities()
    {
        var candidate = TestData.Item();
        var s1 = TestData.Item();
        var s2 = TestData.Item();

        var context = new OutfitGenerationContext
        {
            SelectedItems = { s1, s2 },
            PairCompatibility = new Dictionary<(Guid, Guid), double>
            {
                [ItemPair.Canonical(candidate.Id, s1.Id)] = 1.0,
                [ItemPair.Canonical(candidate.Id, s2.Id)] = 0.0,
            },
        };

        Assert.Equal(1.15, _sut.Evaluate(candidate, context), 3);
    }

    [Fact]
    public void Evaluate_FloorsStrongNegativePair_AboveVetoThreshold()
    {
        var candidate = TestData.Item();
        var selected = TestData.Item();
        var context = new OutfitGenerationContext
        {
            SelectedItems = { selected },
            PairCompatibility = new Dictionary<(Guid, Guid), double> { [ItemPair.Canonical(candidate.Id, selected.Id)] = -1.0 },
        };

        Assert.Equal(0.7, _sut.Evaluate(candidate, context), 3);
    }

    [Fact]
    public void Evaluate_IgnoresSelfPairing()
    {
        var candidate = TestData.Item();
        var context = new OutfitGenerationContext
        {
            SelectedItems = { candidate },
            PairCompatibility = new Dictionary<(Guid, Guid), double> { [ItemPair.Canonical(candidate.Id, candidate.Id)] = 1.0 },
        };

        Assert.Equal(1.0, _sut.Evaluate(candidate, context), 3);
    }
}
