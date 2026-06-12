using WardrobeManager.Infrastructure.ExternalServices;

namespace WardrobeManager.Tests.Unit.Classifiers;

[Trait("Category", "Unit")]
public sealed class GarmentClassifierTests : IDisposable
{
    private readonly string _mapPath;
    private readonly GarmentClassifier _sut;

    public GarmentClassifierTests()
    {
        _mapPath = Path.Combine(Path.GetTempPath(), $"garment-map-{Guid.NewGuid():N}.json");
        File.WriteAllText(_mapPath, """
        {
          "shorts": ["shorts", "pantaloni scurti"],
          "jeans": ["jeans", "blugi"]
        }
        """);
        _sut = new GarmentClassifier(_mapPath);
    }

    public void Dispose() => File.Delete(_mapPath);

    [Fact]
    public void Detect_FindsSingleGarment()
    {
        var result = _sut.Detect("I want some shorts");
        Assert.Single(result);
        Assert.Equal("shorts", result[0].SubType);
    }

    [Fact]
    public void Detect_OrdersGarmentsByAppearance_AndFoldsDiacritics()
    {
        // "blugi" (jeans) appears before "pantaloni scurți" (shorts); diacritics are folded.
        var result = _sut.Detect("blugi si pantaloni scurți");
        Assert.Equal(new[] { "jeans", "shorts" }, result.Select(g => g.SubType).ToArray());
    }

    [Theory]
    [InlineData("nothing relevant here")]
    [InlineData("")]
    public void Detect_ReturnsEmpty_WhenNoMatch(string prompt)
        => Assert.Empty(_sut.Detect(prompt));

    [Fact]
    public void Detect_ReturnsEmpty_WhenMapFileMissing()
    {
        var classifier = new GarmentClassifier(Path.Combine(Path.GetTempPath(), "missing-garments.json"));
        Assert.Empty(classifier.Detect("shorts and jeans"));
    }
}
