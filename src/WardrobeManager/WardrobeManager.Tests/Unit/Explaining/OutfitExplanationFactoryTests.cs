using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Explaining;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Explaining;

[Trait("Category", "Unit")]
public sealed class OutfitExplanationFactoryTests
{
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private readonly IWeatherService _weather = Substitute.For<IWeatherService>();

    private Task<OutfitExplanation> Build(
        IReadOnlyList<Guid> ids,
        IEnumerable<IOutfitEvaluator>? evaluators = null,
        string? city = null,
        IReadOnlyList<string>? tradeoffs = null)
        => OutfitExplanationFactory.BuildAsync(
            _clothing, _weather, evaluators ?? Array.Empty<IOutfitEvaluator>(),
            NullLogger.Instance, ids, style: "casual", occasion: "work", city: city,
            tradeoffs: tradeoffs, CancellationToken.None);

    [Fact]
    public async Task BuildAsync_ReturnsEmpty_WhenNoItemsResolve()
    {
        _clothing.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
                 .Returns(new List<ClothingItem>());

        var result = await Build(new[] { Guid.NewGuid() });

        Assert.Empty(result.Pieces);
    }

    [Fact]
    public async Task BuildAsync_DescribesPieces_FromAttributes()
    {
        var top = new ClothingItem
        {
            Id = Guid.NewGuid(), Type = ClothingType.Top, Name = "Tee",
            Color = "Blue", Material = "Cotton", SubType = "T-Shirt", Usage = "Casual",
        };
        _clothing.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
                 .Returns(new List<ClothingItem> { top });

        var result = await Build(new[] { top.Id }, tradeoffs: new[] { "limited shoes" });

        var piece = Assert.Single(result.Pieces);
        Assert.Equal("top", piece.Slot);
        Assert.Equal("blue cotton t-shirt", piece.Description);
        Assert.Equal("casual", result.Style);
        Assert.Equal("limited shoes", Assert.Single(result.Tradeoffs));
    }

    [Fact]
    public async Task BuildAsync_FallsBackToName_WhenNoAttributes()
    {
        var item = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Accessory, Name = "Lucky belt" };
        _clothing.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
                 .Returns(new List<ClothingItem> { item });

        var result = await Build(new[] { item.Id });

        Assert.Equal("Lucky belt", Assert.Single(result.Pieces).Description);
    }

    [Fact]
    public async Task BuildAsync_AddsHighlights_ForStrongEvaluatorSignals()
    {
        var top = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top, Name = "Tee" };
        _clothing.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
                 .Returns(new List<ClothingItem> { top });

        var result = await Build(new[] { top.Id }, evaluators: new[] { new FakeEvaluator("Style", 0.9) });

        Assert.Equal("fits the requested style", Assert.Single(result.Pieces).Highlights.Single());
    }

    [Fact]
    public async Task BuildAsync_ContinuesWithoutWeather_WhenLookupFails()
    {
        var top = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top, Name = "Tee" };
        _clothing.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
                 .Returns(new List<ClothingItem> { top });
        _weather.GetCurrentWeatherAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns<WeatherData>(_ => throw new InvalidOperationException("no key"));

        var result = await Build(new[] { top.Id }, city: "Rome");

        Assert.Null(result.Weather);
        Assert.Single(result.Pieces);
    }

    private sealed class FakeEvaluator(string name, double score) : IOutfitEvaluator
    {
        public string Name => name;
        public double Evaluate(ClothingItem candidate, OutfitGenerationContext context) => score;
    }
}
