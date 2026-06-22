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

    [Fact]
    public void Ground_ReturnsEmptyText_ForBlankFields()
    {
        var narrative = new StylistOutfit([1], "   ", [string.Empty], "");

        var result = StylistNarrativeGrounder.Ground(
            narrative,
            new List<ClothingItem> { new() { Type = ClothingType.Bottom, SubType = "jeans", Color = "khaki" } });

        Assert.Equal(string.Empty, result.Headline);
        Assert.Equal(string.Empty, Assert.Single(result.Highlights));
        Assert.Equal(string.Empty, result.StylingTip);
    }

    [Fact]
    public void Ground_LeavesTextUnchanged_WhenItemHasNoColor()
    {
        var selected = new List<ClothingItem> { new() { Type = ClothingType.Bottom, SubType = "jeans", Color = null } };
        var narrative = new StylistOutfit([1], "Black jeans anchor it", [], "");

        var result = StylistNarrativeGrounder.Ground(narrative, selected);

        Assert.Equal("Black jeans anchor it", result.Headline);
    }

    [Fact]
    public void Ground_UsesFirstColor_WhenItemListsSeveral()
    {
        var selected = new List<ClothingItem> { new() { Type = ClothingType.Bottom, SubType = "jeans", Color = "olive, black" } };
        var narrative = new StylistOutfit([1], "Black jeans anchor it", [], "");

        var result = StylistNarrativeGrounder.Ground(narrative, selected);

        Assert.Equal("Olive jeans anchor it", result.Headline);
    }

    [Fact]
    public void Ground_PreservesAllCaps_WhenOriginalIsUppercased()
    {
        var selected = new List<ClothingItem> { new() { Type = ClothingType.Bottom, SubType = "jeans", Color = "khaki" } };
        var narrative = new StylistOutfit([1], "BLACK JEANS for the win", [], "");

        var result = StylistNarrativeGrounder.Ground(narrative, selected);

        Assert.StartsWith("KHAKI JEANS", result.Headline);
    }

    [Fact]
    public void Ground_HandlesLongInput_WithinRegexTimeout()
    {
        // a long, repetitive sentence that still completes well under the 200ms guard.
        var text = string.Concat(Enumerable.Repeat("the black shirt and ", 200)) + "done";
        var selected = new List<ClothingItem> { new() { Type = ClothingType.Top, SubType = "shirt", Color = "white" } };

        var result = StylistNarrativeGrounder.Ground(new StylistOutfit([1], text, [], ""), selected);

        Assert.Contains("white shirt", result.Headline);
        Assert.DoesNotContain("black shirt", result.Headline);
    }
}
