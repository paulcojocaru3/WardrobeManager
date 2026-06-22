using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Generation;

[Trait("Category", "Unit")]
public sealed class StylistOutfitComposerTests
{
    [Fact]
    public async Task ComposeAsync_ReranksGemmaOutfits_WithPairingsAndLearnedTaste()
    {
        var userId = Guid.NewGuid();
        var stylist = Substitute.For<IOutfitStylist>();
        var pairs = Substitute.For<IItemPairScoreRepository>();
        var profiles = Substitute.For<IUserLearningProfileRepository>();

        var rejectedTop = Item(ClothingType.Top, "red", "Casual");
        var preferredTop = Item(ClothingType.Top, "green", "Casual");
        var bottom = Item(ClothingType.Bottom, "black", "Casual");
        var shoes = Item(ClothingType.Shoes, "white", "Casual");

        stylist.ComposeAsync(
                Arg.Any<IReadOnlyList<StylistItem>>(),
                Arg.Any<StylistContext>(),
                Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new StylistOutfit(new[] { 1, 3, 4 }, "First", [], ""),
                new StylistOutfit(new[] { 2, 3, 4 }, "Learned", [], ""),
            });

        pairs.GetCompatibilityMapAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<(Guid, Guid), double>
            {
                [ItemPair.Canonical(rejectedTop.Id, bottom.Id)] = -0.8,
                [ItemPair.Canonical(preferredTop.Id, bottom.Id)] = 0.8,
            });
        profiles.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserLearningProfile
            {
                UserId = userId,
                ColorScores = new Dictionary<string, double> { ["red"] = 0.1, ["green"] = 0.9 },
            });

        var sut = new StylistOutfitComposer(
            stylist, pairs, profiles, NullLogger<StylistOutfitComposer>.Instance);

        var result = await sut.ComposeAsync(
            userId,
            [rejectedTop, preferredTop, bottom, shoes],
            new StylistContext("casual", "day", null),
            seed: null,
            lockSeed: false,
            shuffle: false,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains(result.ChosenItems, item => item.Id == preferredTop.Id);
        Assert.DoesNotContain(result.ChosenItems, item => item.Id == rejectedTop.Id);
    }

    private static ClothingItem Item(ClothingType type, string color, string usage) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        Color = color,
        Usage = usage,
        Name = $"{color} {type}",
    };
}
