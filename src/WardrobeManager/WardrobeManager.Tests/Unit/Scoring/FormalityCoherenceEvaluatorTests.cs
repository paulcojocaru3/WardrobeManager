using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Scoring;

[Trait("Category", "Unit")]
public sealed class FormalityCoherenceEvaluatorTests
{
    private readonly FormalityCoherenceEvaluator _sut = new();

    private static ClothingItem WithFormality(int? formality, string? usage = null)
        => new() { Id = Guid.NewGuid(), Type = ClothingType.Top, Formality = formality, Usage = usage };

    [Fact]
    public void Evaluate_Abstains_WhenCandidateUnenriched()
    {
        var candidate = WithFormality(formality: null, usage: null);
        var context = new OutfitGenerationContext { OutfitFormalityRank = 2 };

        Assert.Equal(1.0, _sut.Evaluate(candidate, context));
    }

    [Fact]
    public void Evaluate_Abstains_WhenNoAnchorAvailable()
    {
        var candidate = WithFormality(formality: 3);
        var context = new OutfitGenerationContext { SelectedItems = new List<ClothingItem>() };

        Assert.Equal(1.0, _sut.Evaluate(candidate, context));
    }

    [Fact]
    public void Evaluate_GivesTopScore_WhenSameRankAsAnchor()
    {
        var candidate = WithFormality(formality: 3); // rank 2
        var context = new OutfitGenerationContext { OutfitFormalityRank = 2 };

        Assert.Equal(1.5, _sut.Evaluate(candidate, context), 3);
    }

    [Fact]
    public void Evaluate_PenalizesProgressively_AsRankDiverges()
    {
        var context0 = new OutfitGenerationContext { OutfitFormalityRank = 0 };

        var sameDiff = _sut.Evaluate(WithFormality(formality: 1), context0); // diff 0
        var oneApart = _sut.Evaluate(WithFormality(formality: 2), context0); // diff 1
        var twoApart = _sut.Evaluate(WithFormality(formality: 3), context0); // diff 2

        Assert.True(sameDiff > oneApart);
        Assert.True(oneApart > twoApart);
        Assert.True(twoApart >= 0.05);
    }

    [Fact]
    public void Evaluate_UsesMedianOfSelectedItems_WhenNoExplicitRank()
    {
        var context = new OutfitGenerationContext
        {
            SelectedItems = new List<ClothingItem>
            {
                WithFormality(formality: 5), // rank 4
                WithFormality(formality: 5),
            }
        };

        // candidate also rank 4 -> coherent, top score
        Assert.Equal(1.5, _sut.Evaluate(WithFormality(formality: 5), context), 3);
    }
}
