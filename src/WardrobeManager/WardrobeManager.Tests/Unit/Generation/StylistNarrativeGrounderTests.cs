using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Generation;

[Trait("Category", "Unit")]
public sealed class StylistNarrativeGrounderTests
{
    [Fact]
    public void CandidatePrompt_UsesCanonicalColor_NotStaleName()
    {
        var jeans = new ClothingItem
        {
            Id = Guid.NewGuid(),
            Type = ClothingType.Bottom,
            Name = "black jeans",
            SubType = "jeans",
            Color = "khaki"
        };

        var line = Assert.Single(StylistCandidateSet.Build([jeans]).Lines).Line;

        Assert.Contains("subtype=jeans", line);
        Assert.Contains("color=khaki", line);
        Assert.DoesNotContain("black jeans", line, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ground_CorrectsColorForTheMentionedGarmentSlot()
    {
        var selected = new List<ClothingItem>
        {
            new() { Type = ClothingType.Bottom, SubType = "jeans", Color = "khaki" },
            new() { Type = ClothingType.Shoes, SubType = "sneakers", Color = "black" }
        };
        var narrative = new StylistOutfit(
            [1, 2],
            "Black denim balance",
            ["Black jeans anchor the outfit while the black sneakers keep it grounded."],
            "Cuff the black jeans once.");

        var result = StylistNarrativeGrounder.Ground(narrative, selected);

        Assert.Equal("Khaki denim balance", result.Headline);
        Assert.Contains("Khaki jeans", Assert.Single(result.Highlights));
        Assert.Contains("black sneakers", Assert.Single(result.Highlights));
        Assert.Equal("Cuff the khaki jeans once.", result.StylingTip);
    }
}
