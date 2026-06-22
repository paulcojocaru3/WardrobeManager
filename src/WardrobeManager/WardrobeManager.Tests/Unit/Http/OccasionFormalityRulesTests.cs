using WardrobeManager.Infrastructure.ExternalServices;

namespace WardrobeManager.Tests.Unit.Http;

[Trait("Category", "Unit")]
public sealed class OccasionFormalityRulesTests
{
    private static OccasionFormalityRules Defaults() => new("does-not-exist.json");

    [Theory]
    [InlineData("gym", 1)]
    [InlineData("casual", 2)]
    [InlineData("work", 3)]
    [InlineData("party", 4)]
    [InlineData("wedding", 5)]
    public void FormalityFor_MapsKnownOccasions(string occasion, int expected)
    {
        Assert.Equal(expected, Defaults().FormalityFor(occasion));
    }

    [Fact]
    public void FormalityFor_IsCaseAndWhitespaceInsensitive()
    {
        Assert.Equal(5, Defaults().FormalityFor("  FORMAL  "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FormalityFor_ReturnsNull_ForBlankInput(string? occasion)
    {
        Assert.Null(Defaults().FormalityFor(occasion));
    }

    [Fact]
    public void FormalityFor_ReturnsNull_ForUnknownOccasion()
    {
        Assert.Null(Defaults().FormalityFor("spelunking"));
    }

    [Fact]
    public void Ctor_LoadsBucketsFromConfigFile_AndClampsValues()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """{ "buckets": { "brunch": 3, "gala": 9 } }""");
            var rules = new OccasionFormalityRules(path);

            Assert.Equal(3, rules.FormalityFor("brunch"));
            Assert.Equal(5, rules.FormalityFor("gala")); // 9 clamped to 5
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Ctor_FallsBackToDefaults_OnMalformedConfigFile()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "{ not valid json");
            var rules = new OccasionFormalityRules(path);

            Assert.Equal(2, rules.FormalityFor("casual")); // default still applies
        }
        finally
        {
            File.Delete(path);
        }
    }
}
