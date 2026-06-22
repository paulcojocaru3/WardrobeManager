using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Commands;
using WardrobeManager.Application.Outfits.Feasibility;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Handlers;

// exercises the gemma3-only synchronous styling path (GenerateWithStylistOnlyAsync).
[Trait("Category", "Unit")]
public sealed class GenerateAiStylistPathTests
{
    private readonly IOutfitGenerator _generator = Substitute.For<IOutfitGenerator>();
    private readonly IWeatherService _weather = Substitute.For<IWeatherService>();
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IOutfitFeedbackRepository _feedback = Substitute.For<IOutfitFeedbackRepository>();
    private readonly IOccasionFormalityRules _occasion = Substitute.For<IOccasionFormalityRules>();
    private readonly IOutfitStylist _stylist = Substitute.For<IOutfitStylist>();
    private readonly IItemPairScoreRepository _pairScores = Substitute.For<IItemPairScoreRepository>();
    private readonly IUserLearningProfileRepository _profiles = Substitute.For<IUserLearningProfileRepository>();
    private readonly IMlService _ml = Substitute.For<IMlService>();
    private readonly IThermalRules _thermal = Substitute.For<IThermalRules>();
    private readonly Guid _userId = Guid.NewGuid();

    private GenerateAiOutfitCommandHandler Sut(bool enabled = true) => new(
        _generator, _weather, _clothing, _users, _feedback, _occasion,
        new StylistOutfitComposer(_stylist, _pairScores, _profiles, NullLogger<StylistOutfitComposer>.Instance),
        new StylistCandidatePoolBuilder(_clothing, _ml, _thermal, NullLogger<StylistCandidatePoolBuilder>.Instance),
        new StylistSettings { Enabled = enabled },
        NullLogger<GenerateAiOutfitCommandHandler>.Instance);

    private static ClothingItem Item(ClothingType type, float[] emb)
        => new() { Id = Guid.NewGuid(), Type = type, Usage = "Casual", Color = "blue", Name = type.ToString(), Embedding = emb };

    private void StubWardrobeRetrieval(List<ClothingItem> wardrobe)
    {
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
              .Returns(new User { Id = _userId, UseGemmaStylistForOutfits = true });
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(wardrobe);
        _clothing.GetWearRecencyAsync(_userId, Arg.Any<CancellationToken>()).Returns(new Dictionary<Guid, DateTime>());
        _feedback.GetRecentlyShownItemIdsAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<ClothingType?>(), Arg.Any<CancellationToken>())
                 .Returns(Array.Empty<Guid>());
        _ml.EmbedTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new[] { 1f, 0f, 0f });
        _clothing.GetSimilarItemsAsync(_userId, Arg.Any<float[]>(), Arg.Any<ClothingType?>(),
                Arg.Any<int>(), Arg.Any<double?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var type = call.ArgAt<ClothingType?>(2);
                return wardrobe.Where(i => i.Type == type).Select(i => (i, 0.9)).ToList();
            });
        _pairScores.GetCompatibilityMapAsync(_userId, Arg.Any<CancellationToken>())
                   .Returns(new Dictionary<(Guid, Guid), double>());
    }

    // returns one valid top+bottom+shoes by reading the numbers the composer actually assigned.
    private void StubStylistPicksValidLook()
    {
        _stylist.ComposeAsync(Arg.Any<IReadOnlyList<StylistItem>>(), Arg.Any<StylistContext>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var lines = call.Arg<IReadOnlyList<StylistItem>>();
                int Num(string slot) => lines.First(l => l.Slot == slot).Number;
                IReadOnlyList<StylistOutfit> outfits = new[]
                {
                    new StylistOutfit(new[] { Num("TOP"), Num("BOTTOM"), Num("SHOES") }, "Crisp Casual", new[] { "neutral palette" }, "tuck the tee"),
                };
                return outfits;
            });
    }

    [Fact]
    public async Task Handle_UsesStylist_AndReturnsStylistComposedOutfit()
    {
        var top = Item(ClothingType.Top, new[] { 1f, 0f, 0f });
        var wardrobe = new List<ClothingItem>
        {
            top,
            Item(ClothingType.Bottom, new[] { 0f, 1f, 0f }),
            Item(ClothingType.Shoes, new[] { 0f, 0f, 1f }),
        };
        StubWardrobeRetrieval(wardrobe);
        StubStylistPicksValidLook();

        var result = await Sut().Handle(new GenerateAiOutfitCommand(_userId, top.Id), CancellationToken.None);

        Assert.True(result.GeneratedByStylist);
        Assert.True(result.IsValid);
        Assert.Equal("Crisp Casual", result.Name);
        Assert.Equal(3, result.SelectedItems.Count);
        await _generator.DidNotReceive().GenerateAiOutfitAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<OutfitGenerationOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenStylistDisabledButUserOptedIn()
    {
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
              .Returns(new User { Id = _userId, UseGemmaStylistForOutfits = true });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Sut(enabled: false).Handle(new GenerateAiOutfitCommand(_userId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Throws_WhenWardrobeEmpty()
    {
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
              .Returns(new User { Id = _userId, UseGemmaStylistForOutfits = true });
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<ClothingItem>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Sut().Handle(new GenerateAiOutfitCommand(_userId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Throws_WhenStylistCannotCompose()
    {
        var top = Item(ClothingType.Top, new[] { 1f, 0f, 0f });
        var wardrobe = new List<ClothingItem>
        {
            top,
            Item(ClothingType.Bottom, new[] { 0f, 1f, 0f }),
            Item(ClothingType.Shoes, new[] { 0f, 0f, 1f }),
        };
        StubWardrobeRetrieval(wardrobe);
        _stylist.ComposeAsync(Arg.Any<IReadOnlyList<StylistItem>>(), Arg.Any<StylistContext>(), Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<StylistOutfit>?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Sut().Handle(new GenerateAiOutfitCommand(_userId, top.Id), CancellationToken.None));
    }
}
