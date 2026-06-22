using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;
using WardrobeManager.Tests.Unit.TestSupport;

namespace WardrobeManager.Tests.Unit.Generation;

[Trait("Category", "Unit")]
public sealed class StylistCandidatePoolBuilderTests
{
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private readonly IMlService _ml = Substitute.For<IMlService>();
    private readonly Guid _userId = Guid.NewGuid();

    private StylistCandidatePoolBuilder Sut() =>
        new(_clothing, _ml, Defaults.Thermal, NullLogger<StylistCandidatePoolBuilder>.Instance);

    [Fact]
    public async Task BuildAsync_UsesFashionClipTextRetrieval_PerSlot()
    {
        _ml.EmbedTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new[] { 1f, 0f, 0f });

        var top = Item(ClothingType.Top, "top", new[] { 1f, 0f, 0f });
        var bottom = Item(ClothingType.Bottom, "bottom", new[] { 0f, 1f, 0f });
        var shoes = Item(ClothingType.Shoes, "shoes", new[] { 0f, 0f, 1f });
        var wardrobe = new List<ClothingItem> { top, bottom, shoes };

        _clothing.GetSimilarItemsAsync(_userId, Arg.Any<float[]>(), Arg.Any<ClothingType?>(),
                Arg.Any<int>(), Arg.Any<double?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var type = call.ArgAt<ClothingType?>(2);
                return wardrobe.Where(i => i.Type == type).Select(i => (i, 0.9)).ToList();
            });

        var result = await Sut().BuildAsync(
            new StylistCandidatePoolRequest(_userId, "office", "Smart Casual", 3, 20, true, null, 24),
            wardrobe,
            new Dictionary<Guid, DateTime>(),
            new HashSet<Guid>(),
            0.7);

        Assert.Contains(result, i => i.Id == top.Id);
        Assert.Contains(result, i => i.Id == bottom.Id);
        Assert.Contains(result, i => i.Id == shoes.Id);
        await _ml.Received().EmbedTextAsync(Arg.Is<string>(q => q.Contains("top")), Arg.Any<CancellationToken>());
        await _ml.Received().EmbedTextAsync(Arg.Is<string>(q => q.Contains("bottom")), Arg.Any<CancellationToken>());
        await _ml.Received().EmbedTextAsync(Arg.Is<string>(q => q.Contains("shoes")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BuildAsync_FallsBackToWardrobe_WhenTextEmbeddingUnavailable()
    {
        _ml.EmbedTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<float>());

        var top = Item(ClothingType.Top, "top", new[] { 1f, 0f, 0f });
        var bottom = Item(ClothingType.Bottom, "bottom", new[] { 0f, 1f, 0f });
        var shoes = Item(ClothingType.Shoes, "shoes", new[] { 0f, 0f, 1f });
        var wardrobe = new List<ClothingItem> { top, bottom, shoes };

        var result = await Sut().BuildAsync(
            new StylistCandidatePoolRequest(_userId, "casual", "Casual", 2, 18, false, null, 24),
            wardrobe,
            new Dictionary<Guid, DateTime>(),
            new HashSet<Guid>(),
            0.7);

        Assert.Contains(result, i => i.Id == top.Id);
        Assert.Contains(result, i => i.Id == bottom.Id);
        Assert.Contains(result, i => i.Id == shoes.Id);
        await _clothing.DidNotReceive().GetSimilarItemsAsync(
            Arg.Any<Guid>(), Arg.Any<float[]>(), Arg.Any<ClothingType?>(), Arg.Any<int>(),
            Arg.Any<double?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BuildAsync_DropsOuterwear_WhenPolicyDisallowsIt()
    {
        _ml.EmbedTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<float>());

        var jacket = Item(ClothingType.Outerwear, "jacket", new[] { 1f, 0f, 0f });
        var wardrobe = new List<ClothingItem>
        {
            Item(ClothingType.Top, "top", new[] { 1f, 0f, 0f }),
            Item(ClothingType.Bottom, "bottom", new[] { 0f, 1f, 0f }),
            Item(ClothingType.Shoes, "shoes", new[] { 0f, 0f, 1f }),
            jacket
        };

        var result = await Sut().BuildAsync(
            new StylistCandidatePoolRequest(_userId, "casual", "Casual", 2, 26, false, null, 24),
            wardrobe,
            new Dictionary<Guid, DateTime>(),
            new HashSet<Guid>(),
            0.7);

        Assert.DoesNotContain(result, i => i.Id == jacket.Id);
    }

    [Fact]
    public async Task BuildAsync_ExcludesAvoidedColorFamilies()
    {
        _ml.EmbedTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<float>());

        var redTop = Item(ClothingType.Top, "red top", new[] { 1f, 0f, 0f });
        redTop.Color = "burgundy red";
        var safeTop = Item(ClothingType.Top, "safe top", new[] { 0f, 1f, 0f });
        safeTop.Color = "white";
        var wardrobe = new List<ClothingItem>
        {
            redTop,
            safeTop,
            Item(ClothingType.Bottom, "bottom", new[] { 0f, 0f, 1f }),
            Item(ClothingType.Shoes, "shoes", new[] { 1f, 1f, 0f }),
        };

        var result = await Sut().BuildAsync(
            new StylistCandidatePoolRequest(
                _userId, "casual", "Casual", 2, 18, false, null, 24,
                FavoriteColors: new[] { "white" }, AvoidColors: new[] { "red" }),
            wardrobe,
            new Dictionary<Guid, DateTime>(),
            new HashSet<Guid>(),
            0.7);

        Assert.DoesNotContain(result, item => item.Id == redTop.Id);
        Assert.Contains(result, item => item.Id == safeTop.Id);
    }

    private static ClothingItem Item(ClothingType type, string name, float[] embedding) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        Name = name,
        Usage = "Casual",
        Season = "All Seasons",
        ProcessedImageUrl = name,
        Embedding = embedding
    };
}
