using WardrobeManager.Application.Outfits.Scoring;

namespace WardrobeManager.Tests.Unit.Scoring;

[Trait("Category", "Unit")]
public sealed class ColorFamilyTests
{
    [Theory]
    [InlineData("black")]
    [InlineData("White")]
    [InlineData("navy")]      // intentionally treated as neutral
    [InlineData("brown")]
    [InlineData("midnight black")] // substring match
    public void IsNeutral_ReturnsTrue_ForNeutralColors(string color)
    {
        Assert.True(ColorFamily.IsNeutral(color));
    }

    [Theory]
    [InlineData("red")]
    [InlineData("emerald")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsNeutral_ReturnsFalse_ForNonNeutralOrBlank(string? color)
    {
        Assert.False(ColorFamily.IsNeutral(color));
    }

    [Theory]
    [InlineData("red", "red")]
    [InlineData("maroon", "red")]
    [InlineData("sky blue", "blue")]
    [InlineData("emerald", "green")]
    [InlineData("lavender", "purple")]
    [InlineData("salmon", "pink")]
    [InlineData("mustard", "yellow")]
    [InlineData("rust", "orange")]
    public void FamilyOf_MapsNonNeutralColorToHueFamily(string color, string expected)
    {
        Assert.Equal(expected, ColorFamily.FamilyOf(color));
    }

    [Theory]
    [InlineData("navy")]        // neutral -> null
    [InlineData("black")]       // neutral -> null
    [InlineData("chartreuse")]  // unknown -> null
    [InlineData("")]
    [InlineData(null)]
    public void FamilyOf_ReturnsNull_ForNeutralBlankOrUnknown(string? color)
    {
        Assert.Null(ColorFamily.FamilyOf(color));
    }

    [Theory]
    [InlineData("navy", "blue")]        // shade -> basic color
    [InlineData("charcoal", "black")]
    [InlineData("off-white", "white")]
    [InlineData("navy blue", "blue")]   // direct substring
    [InlineData("blue", "blue")]        // identical
    [InlineData("Navy", "BLUE")]        // case-insensitive
    public void ColorsMatch_True_ForShadesAndSubstrings(string itemColor, string promptColor)
    {
        Assert.True(ColorFamily.ColorsMatch(itemColor, promptColor));
    }

    [Theory]
    [InlineData("red", "blue")]
    [InlineData("green", "black")]
    [InlineData("navy", "green")]
    [InlineData("chartreuse", "blue")] // unknown shade -> no family match
    [InlineData(null, "blue")]
    [InlineData("blue", "")]
    public void ColorsMatch_False_ForDifferentOrBlankColors(string? itemColor, string? promptColor)
    {
        Assert.False(ColorFamily.ColorsMatch(itemColor, promptColor));
    }
}
