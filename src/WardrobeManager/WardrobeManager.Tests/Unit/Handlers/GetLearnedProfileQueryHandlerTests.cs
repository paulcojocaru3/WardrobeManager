using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public sealed class GetLearnedProfileQueryHandlerTests
{
    private readonly IUserLearningProfileRepository _profiles = Substitute.For<IUserLearningProfileRepository>();
    private readonly IItemPairScoreRepository _pairs = Substitute.For<IItemPairScoreRepository>();
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private readonly Guid _userId = Guid.NewGuid();

    private GetLearnedProfileQueryHandler Sut() => new(_profiles, _pairs, _clothing);

    private void StubPairs(IReadOnlyDictionary<(Guid, Guid), double> map)
        => _pairs.GetCompatibilityMapAsync(_userId, Arg.Any<CancellationToken>()).Returns(map);

    [Fact]
    public async Task Handle_ReturnsEmptyProfile_WhenNothingLearned()
    {
        _profiles.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((UserLearningProfile?)null);
        StubPairs(new Dictionary<(Guid, Guid), double>());

        var result = await Sut().Handle(new GetLearnedProfileQuery(_userId), CancellationToken.None);

        Assert.Empty(result.TopColors);
        Assert.Empty(result.TopStyles);
        Assert.Empty(result.AvoidedColors);
        Assert.Empty(result.StrongPairs);
        Assert.Null(result.UpdatedAt);
    }

    [Fact]
    public async Task Handle_SplitsColorsIntoLikedAndAvoided_ByThreshold()
    {
        var updated = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        _profiles.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new UserLearningProfile
        {
            UserId = _userId,
            ColorScores = new() { ["navy"] = 0.8, ["beige"] = 0.6, ["neutral"] = 0.5, ["orange"] = 0.2 },
            StyleScores = new() { ["minimal"] = 0.7 },
            UpdatedAt = updated,
        });
        StubPairs(new Dictionary<(Guid, Guid), double>());

        var result = await Sut().Handle(new GetLearnedProfileQuery(_userId), CancellationToken.None);

        Assert.Equal(new[] { "navy", "beige" }, result.TopColors.Select(c => c.Label));
        Assert.Equal("orange", Assert.Single(result.AvoidedColors).Label);
        Assert.Equal("minimal", Assert.Single(result.TopStyles).Label);
        Assert.Equal(updated, result.UpdatedAt);
    }

    [Fact]
    public async Task Handle_ResolvesStrongPairNames_AndSkipsDeletedItems()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var deleted = Guid.NewGuid();
        _profiles.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((UserLearningProfile?)null);
        StubPairs(new Dictionary<(Guid, Guid), double>
        {
            [(a, b)] = 0.9,
            [(a, deleted)] = 0.7,   // partner deleted -> dropped
        });
        _clothing.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ClothingItem>
            {
                new() { Id = a, Name = "Tee" },
                new() { Id = b, Name = "Chinos" },
            });

        var result = await Sut().Handle(new GetLearnedProfileQuery(_userId), CancellationToken.None);

        var pair = Assert.Single(result.StrongPairs);
        Assert.Equal("Tee", pair.ItemA);
        Assert.Equal("Chinos", pair.ItemB);
    }

    [Fact]
    public async Task Handle_IgnoresNonPositivePairs()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        _profiles.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((UserLearningProfile?)null);
        StubPairs(new Dictionary<(Guid, Guid), double> { [(a, b)] = -0.5 });

        var result = await Sut().Handle(new GetLearnedProfileQuery(_userId), CancellationToken.None);

        Assert.Empty(result.StrongPairs);
        await _clothing.DidNotReceive().GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
    }
}
