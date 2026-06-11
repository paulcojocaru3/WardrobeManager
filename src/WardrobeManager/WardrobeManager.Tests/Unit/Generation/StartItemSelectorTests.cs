using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Application.Outfits.Prompting;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Generation;

[Trait("Category", "Unit")]
public sealed class StartItemSelectorTests
{
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private readonly IMlService _ml = Substitute.For<IMlService>();
    private readonly Guid _userId = Guid.NewGuid();

    private StartItemSelector Sut() => new(_clothing, _ml, NullLogger<StartItemSelector>.Instance);

    private void GivenEmbedding(params float[] vector)
        => _ml.EmbedTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(vector);

    private void GivenSimilar(params (ClothingItem item, double sim)[] pool)
        => _clothing.GetSimilarItemsAsync(Arg.Any<Guid>(), Arg.Any<float[]>(), Arg.Any<ClothingType?>(),
                Arg.Any<int>(), Arg.Any<double?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(pool.Select(p => (p.item, p.sim)).ToList());

    [Fact]
    public async Task SelectAsync_EmbeddingPath_PrefersStyleAndColorMatch()
    {
        GivenEmbedding(1f, 2f, 3f);
        var formalBlack = new ClothingItem { Id = Guid.NewGuid(), Usage = "Formal", Color = "black" };
        var casualWhite = new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual", Color = "white" };
        GivenSimilar((formalBlack, 0.9), (casualWhite, 0.8));

        var intent = new PromptIntent { AnchorDescription = "white shirt", DesiredColors = new[] { "white" }, Style = "Casual" };
        var result = await Sut().SelectAsync(_userId, intent);

        Assert.Equal(casualWhite.Id, result!.Id);
    }

    [Fact]
    public async Task SelectAsync_FallsBack_WhenEmbeddingEmpty()
    {
        GivenEmbedding(Array.Empty<float>());
        var casual = new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual", Color = "blue" };
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<ClothingItem> { casual });

        var result = await Sut().SelectAsync(_userId, new PromptIntent { Style = "Casual" });

        Assert.Equal(casual.Id, result!.Id);
    }

    [Fact]
    public async Task SelectAsync_FallsBack_WhenEmbeddingThrows()
    {
        _ml.EmbedTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Returns<float[]>(_ => throw new HttpRequestException("ml down"));
        var item = new ClothingItem { Id = Guid.NewGuid() };
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<ClothingItem> { item });

        var result = await Sut().SelectAsync(_userId, new PromptIntent { AnchorDescription = "a shirt" });

        Assert.Equal(item.Id, result!.Id);
    }

    [Fact]
    public async Task SelectAsync_FallsBackDirectly_WhenNoQueryText()
    {
        var item = new ClothingItem { Id = Guid.NewGuid() };
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<ClothingItem> { item });

        var result = await Sut().SelectAsync(_userId, new PromptIntent()); // nothing to build a query from

        Assert.Equal(item.Id, result!.Id);
        await _ml.DidNotReceive().EmbedTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SelectAsync_ReturnsNull_WhenWardrobeEmpty()
    {
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<ClothingItem>());
        Assert.Null(await Sut().SelectAsync(_userId, new PromptIntent()));
    }

    [Fact]
    public async Task SelectAsync_HardFiltersBySubType_WhenGarmentRequested()
    {
        GivenEmbedding(1f, 2f);
        var jeans = new ClothingItem { Id = Guid.NewGuid(), SubType = "jeans", Color = "blue" };
        var chinos = new ClothingItem { Id = Guid.NewGuid(), SubType = "chinos", Color = "blue" };
        GivenSimilar((chinos, 0.95), (jeans, 0.9)); // chinos ranks higher but sub-type must win

        var intent = new PromptIntent
        {
            RequestedGarments = new[] { new RequestedGarment("jeans", ClothingType.Bottom) },
            DesiredColors = new[] { "blue" },
        };
        var result = await Sut().SelectAsync(_userId, intent);

        Assert.Equal(jeans.Id, result!.Id);
    }

    [Fact]
    public async Task SelectAsync_RetriesAcrossAllTypes_WhenTypedPoolEmpty()
    {
        GivenEmbedding(1f, 2f);
        var casual = new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual" };
        _clothing.GetSimilarItemsAsync(Arg.Any<Guid>(), Arg.Any<float[]>(), Arg.Any<ClothingType?>(),
                Arg.Any<int>(), Arg.Any<double?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new List<(ClothingItem, double)>(), new List<(ClothingItem, double)> { (casual, 0.9) });

        var intent = new PromptIntent { Style = "Casual", RequestedTypes = new[] { ClothingType.Bottom } };
        var result = await Sut().SelectAsync(_userId, intent);

        Assert.Equal(casual.Id, result!.Id);
    }

    [Fact]
    public async Task SelectAsync_Fallback_FiltersByStyleColorAndType_AndHonorsExclusions()
    {
        GivenEmbedding(Array.Empty<float>()); // force fallback
        var match = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Usage = "Casual", Color = "blue" };
        var excluded = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Usage = "Casual", Color = "blue" };
        var wrong = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top, Usage = "Formal", Color = "red" };
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<ClothingItem> { excluded, match, wrong });

        var intent = new PromptIntent { Style = "Casual", DesiredColors = new[] { "blue" }, RequestedTypes = new[] { ClothingType.Bottom } };
        var result = await Sut().SelectAsync(_userId, intent, new[] { excluded.Id });

        Assert.Equal(match.Id, result!.Id);
    }

    [Fact]
    public async Task SelectAsync_MultipleColors_PrefersSingleColorSeed_NotMultiColorGarment()
    {
        GivenEmbedding(1f, 2f);
        // The bicolor blouse ranks highest by similarity, but the seed should be a single-color piece.
        var bicolor = new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual", Color = "white black" };
        var whiteOnly = new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual", Color = "white" };
        GivenSimilar((bicolor, 0.95), (whiteOnly, 0.9));

        var intent = new PromptIntent { DesiredColors = new[] { "white", "black" }, Style = "Casual" };
        var result = await Sut().SelectAsync(_userId, intent);

        Assert.Equal(whiteOnly.Id, result!.Id);
    }

    [Fact]
    public async Task SelectAsync_MultipleColors_QueriesPrimaryColorOnly()
    {
        GivenEmbedding(1f);
        GivenSimilar((new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual", Color = "white" }, 0.9));

        await Sut().SelectAsync(_userId, new PromptIntent { DesiredColors = new[] { "white", "black" }, Style = "Casual" });

        await _ml.Received().EmbedTextAsync(
            Arg.Is<string>(s => s.Contains("white") && !s.Contains("black")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SelectAsync_NoStyle_PrefersCasualSeed_OverFormal()
    {
        GivenEmbedding(1f, 2f);
        var formal = new ClothingItem { Id = Guid.NewGuid(), Usage = "Formal", Color = "black" };
        var casual = new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual", Color = "black" };
        GivenSimilar((formal, 0.95), (casual, 0.85)); // formal ranks higher by similarity

        var result = await Sut().SelectAsync(_userId, new PromptIntent { DesiredColors = new[] { "black" } });

        Assert.Equal(casual.Id, result!.Id);
    }

    [Fact]
    public async Task SelectAsync_NoStyle_PrefersCasual_OverSmartCasual()
    {
        GivenEmbedding(1f);
        var smartCasual = new ClothingItem { Id = Guid.NewGuid(), Usage = "Smart Casual", Color = "blue" };
        var casual = new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual", Color = "blue" };
        GivenSimilar((smartCasual, 0.95), (casual, 0.9));

        var result = await Sut().SelectAsync(_userId, new PromptIntent { DesiredColors = new[] { "blue" } });

        Assert.Equal(casual.Id, result!.Id);
    }

    [Fact]
    public async Task SelectAsync_NoStyle_FallbackPath_PrefersCasual()
    {
        var formal = new ClothingItem { Id = Guid.NewGuid(), Usage = "Formal" };
        var casual = new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual" };
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new List<ClothingItem> { formal, casual });

        var result = await Sut().SelectAsync(_userId, new PromptIntent()); // no query -> fallback

        Assert.Equal(casual.Id, result!.Id);
    }

    [Fact]
    public async Task SelectAsync_Regeneration_PrefersTopMostDifferentFromExcludedSeeds()
    {
        GivenEmbedding(1f, 0f, 0f);
        var prevSeedId = Guid.NewGuid();
        _clothing.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ClothingItem> { new() { Id = prevSeedId, Usage = "Casual", Embedding = new[] { 1f, 0f, 0f } } });

        var lookalike = new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual", Embedding = new[] { 1f, 0f, 0f } };  // like the previous seed
        var different = new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual", Embedding = new[] { 0f, 1f, 0f } };  // visually different
        GivenSimilar((lookalike, 0.95), (different, 0.9)); // lookalike ranks higher by prompt similarity

        var result = await Sut().SelectAsync(_userId, new PromptIntent { Style = "Casual" }, new[] { prevSeedId });

        Assert.Equal(different.Id, result!.Id); // diversity wins despite lower prompt similarity
    }

    [Fact]
    public async Task SelectAsync_Fallback_Regeneration_PrefersDifferentTop()
    {
        GivenEmbedding(Array.Empty<float>()); // force the fallback path
        var prevSeedId = Guid.NewGuid();
        _clothing.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ClothingItem> { new() { Id = prevSeedId, Usage = "Casual", Embedding = new[] { 1f, 0f, 0f } } });
        var lookalike = new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual", Embedding = new[] { 1f, 0f, 0f } };
        var different = new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual", Embedding = new[] { 0f, 1f, 0f } };
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new List<ClothingItem> { lookalike, different }); // CreatedAt order: lookalike first

        var result = await Sut().SelectAsync(_userId, new PromptIntent { Style = "Casual" }, new[] { prevSeedId });

        Assert.Equal(different.Id, result!.Id); // fallback now diversifies instead of walking the list
    }

    [Fact]
    public async Task SelectAsync_ExcludesWeatherVetoedSeed()
    {
        GivenEmbedding(1f, 0f, 0f);
        var shorts = new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual", SubType = "shorts", Embedding = new[] { 1f, 0f, 0f } };
        var pants = new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual", SubType = "jeans", Embedding = new[] { 1f, 0f, 0f } };
        GivenSimilar((shorts, 0.95), (pants, 0.9)); // shorts ranks higher by similarity

        var result = await Sut().SelectAsync(_userId, new PromptIntent { Style = "Casual" }, null, new WeatherData(5, "Clear", "Winter"));

        Assert.Equal(pants.Id, result!.Id); // shorts vetoed by cold weather
    }

    [Fact]
    public async Task SelectAsync_PerSlotAvoidColors_ExcludesThoseColorSeeds()
    {
        GivenEmbedding(1f, 0f, 0f);
        var black = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top, Usage = "Casual", Color = "black", Embedding = new[] { 1f, 0f, 0f } };
        var blue = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top, Usage = "Casual", Color = "blue", Embedding = new[] { 1f, 0f, 0f } };
        GivenSimilar((black, 0.95), (blue, 0.9)); // black ranks higher but is avoided for the top

        var intent = new PromptIntent
        {
            AnchorDescription = "t-shirt", // gives the embedding path a query (the realistic flow)
            RequestedTypes = new[] { ClothingType.Top },
            GarmentSpecs = new[] { new GarmentSpec { Type = ClothingType.Top, AvoidColors = new[] { "black", "white" } } },
        };
        var result = await Sut().SelectAsync(_userId, intent);

        Assert.Equal(blue.Id, result!.Id);
    }

    [Fact]
    public async Task SelectAsync_PerSlotDesiredColor_PrefersThatColorSeed()
    {
        GivenEmbedding(1f, 0f, 0f);
        var blue = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Usage = "Casual", Color = "blue" };
        var black = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Usage = "Casual", Color = "black" };
        GivenSimilar((blue, 0.95), (black, 0.9)); // blue ranks higher but black is the desired bottom color

        var intent = new PromptIntent
        {
            RequestedTypes = new[] { ClothingType.Bottom },
            GarmentSpecs = new[] { new GarmentSpec { Type = ClothingType.Bottom, DesiredColors = new[] { "black" } } },
        };
        var result = await Sut().SelectAsync(_userId, intent);

        Assert.Equal(black.Id, result!.Id);
    }

    [Fact]
    public async Task SelectAsync_ExcludesAvoidedColorSeed()
    {
        GivenEmbedding(1f, 0f, 0f);
        var red = new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual", Color = "red", Embedding = new[] { 1f, 0f, 0f } };
        var blue = new ClothingItem { Id = Guid.NewGuid(), Usage = "Casual", Color = "blue", Embedding = new[] { 1f, 0f, 0f } };
        GivenSimilar((red, 0.95), (blue, 0.9));

        var result = await Sut().SelectAsync(_userId, new PromptIntent { Style = "Casual", AvoidColors = new[] { "red" } });

        Assert.Equal(blue.Id, result!.Id); // avoided color excluded from seed selection
    }
}
