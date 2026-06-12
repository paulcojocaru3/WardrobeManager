using WardrobeManager.Infrastructure.ExternalServices;

namespace WardrobeManager.Tests.Unit.Classifiers;

[Trait("Category", "Unit")]
public sealed class OccasionClassifierTests : IDisposable
{
    private readonly string _mapPath;
    private readonly OccasionClassifier _sut;

    public OccasionClassifierTests()
    {
        _mapPath = Path.Combine(Path.GetTempPath(), $"occasion-map-{Guid.NewGuid():N}.json");
        File.WriteAllText(_mapPath, """
        {
          "occasions": [
            { "style": "Formal", "keywords": ["wedding", "dinner date"] },
            { "style": "Sporty", "keywords": ["gym"] }
          ]
        }
        """);
        _sut = new OccasionClassifier(_mapPath);
    }

    public void Dispose() => File.Delete(_mapPath);

    [Theory]
    [InlineData("I'm going to a wedding", "Formal")]
    [InlineData("dinner date tonight", "Formal")]   // longest keyword wins
    [InlineData("hitting the gym later", "Sporty")]
    public void ClassifyStyle_ReturnsMappedStyle(string prompt, string expected)
        => Assert.Equal(expected, _sut.ClassifyStyle(prompt));

    [Theory]
    [InlineData("just chilling at home")]
    [InlineData("")]
    [InlineData("   ")]
    public void ClassifyStyle_ReturnsNull_WhenNoKeywordMatches(string prompt)
        => Assert.Null(_sut.ClassifyStyle(prompt));

    [Fact]
    public void ClassifyStyle_ReturnsNull_WhenMapFileMissing()
    {
        var classifier = new OccasionClassifier(Path.Combine(Path.GetTempPath(), "does-not-exist.json"));
        Assert.Null(classifier.ClassifyStyle("wedding"));
    }
}
