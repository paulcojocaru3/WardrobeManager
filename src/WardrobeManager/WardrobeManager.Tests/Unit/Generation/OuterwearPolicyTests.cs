using WardrobeManager.Application.Outfits.Generation;

namespace WardrobeManager.Tests.Unit.Generation;

[Trait("Category", "Unit")]
public sealed class OuterwearPolicyTests
{
    [Fact]
    public void Always_mode_includes_regardless_of_temperature()
    {
        Assert.True(OuterwearPolicy.ShouldIncludeOuterwear("always", 18, 30, null));
    }

    [Fact]
    public void Never_mode_excludes_regardless_of_temperature()
    {
        Assert.False(OuterwearPolicy.ShouldIncludeOuterwear("never", 18, -5, "cold"));
    }

    [Theory]
    [InlineData(24, 18, false)] // warmer than threshold -> no outerwear (the reported bug)
    [InlineData(18, 18, true)]  // at threshold -> include
    [InlineData(10, 18, true)]  // colder than threshold -> include
    public void Auto_mode_compares_temperature_to_threshold(double temp, double threshold, bool expected)
    {
        Assert.Equal(expected, OuterwearPolicy.ShouldIncludeOuterwear(null, threshold, temp, null));
    }

    [Fact]
    public void Auto_mode_without_temperature_uses_hint()
    {
        Assert.True(OuterwearPolicy.ShouldIncludeOuterwear("auto", 18, null, "cold"));
        Assert.False(OuterwearPolicy.ShouldIncludeOuterwear("auto", 18, null, "hot"));
    }

    [Fact]
    public void Auto_mode_without_any_information_does_not_force_outerwear()
    {
        // no weather, no hint -> respect the user's threshold/preference instead of over-adding a layer.
        Assert.False(OuterwearPolicy.ShouldIncludeOuterwear(null, 18, null, null));
    }
}
