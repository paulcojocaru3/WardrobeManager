using FluentValidation;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing.Queries;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public sealed class GetWearStatisticsQueryHandlerTests
{
    private readonly IWearEventRepository _wear = Substitute.For<IWearEventRepository>();
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private readonly IOutfitRepository _outfits = Substitute.For<IOutfitRepository>();
    private readonly Guid _userId = Guid.NewGuid();

    private GetWearStatisticsQueryHandler Sut() => new(_wear, _clothing, _outfits);

    private (List<ClothingItem> clothes, List<Outfit> outfits, List<WearEvent> events) Dataset()
    {
        var top = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top, Color = "Blue", Usage = "Casual", ProcessedImageUrl = "u1" };
        var bottom = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Color = "Black", Usage = "Formal,Smart Casual", ProcessedImageUrl = "u2" };
        var shoes = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Shoes, Color = "White", Usage = "Sports", ProcessedImageUrl = "u3" };
        var neverWorn = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top, Color = null, Usage = null, ProcessedImageUrl = "u4" };

        var aiOutfit = new Outfit { Id = Guid.NewGuid(), Name = "AI Look", IsAiGenerated = true, Items = new() { top, bottom } };
        var customOutfit = new Outfit { Id = Guid.NewGuid(), Name = "Custom Look", IsAiGenerated = false, Items = new() { shoes } };

        var now = DateTime.UtcNow;
        WearEvent Event(ClothingItem item, Guid? outfitId, double daysAgo) => new()
        {
            UserId = _userId,
            ClothingItemId = item.Id,
            ClothingItem = item,
            OutfitId = outfitId,
            WearDate = now.AddDays(-daysAgo),
        };

        // consecutive days (today-3..today) -> streak; mix of AI / custom / no-outfit sessions.
        var events = new List<WearEvent>
        {
            Event(top, aiOutfit.Id, 3),
            Event(bottom, aiOutfit.Id, 2),
            Event(shoes, customOutfit.Id, 1),
            Event(top, customOutfit.Id, 0.08),   // ~today, worn again
            Event(bottom, null, 0.04),            // ~today, custom session (no outfit)
        };

        return (new() { top, bottom, shoes, neverWorn }, new() { aiOutfit, customOutfit }, events);
    }

    [Fact]
    public async Task Handle_BuildsRichStatistics_ForBoundedWindow()
    {
        var (clothes, outfits, events) = Dataset();
        _wear.GetByUserIdAsync(_userId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(events);
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(clothes);
        _outfits.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(outfits);

        var dto = await Sut().Handle(new GetWearStatisticsQuery(_userId, "30d"), CancellationToken.None);

        Assert.Equal(5, dto.TotalWearEvents);
        Assert.Equal(3, dto.TotalDistinctWornItems);       // top, bottom, shoes
        Assert.True(dto.Streak.LongestStreakDays >= 3);
        Assert.NotEmpty(dto.WardrobeColors);
        Assert.NotEmpty(dto.CategoryUtilization);
        Assert.NotEmpty(dto.TopOutfits);
        Assert.NotEmpty(dto.WearHistory);
        Assert.Equal(75, dto.WardrobeUtilizationRate, 0);   // 3 of 4 items worn
        Assert.True(dto.OutfitSourceSplit.TotalSessions > 0);
        Assert.True(dto.OutfitSourceSplit.AiGeneratedSessions > 0);
    }

    [Fact]
    public async Task Handle_UsesAllTimeRepository_WhenNoRange()
    {
        var (clothes, outfits, events) = Dataset();
        _wear.GetAllByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(events);
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(clothes);
        _outfits.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(outfits);

        var dto = await Sut().Handle(new GetWearStatisticsQuery(_userId), CancellationToken.None);

        Assert.Equal("all time", dto.Window.Label);
        Assert.Equal(5, dto.TotalWearEvents);
        await _wear.Received(1).GetAllByUserIdAsync(_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsEarly_WhenWardrobeEmpty()
    {
        var (_, outfits, events) = Dataset();
        _wear.GetAllByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(events);
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<ClothingItem>());
        _outfits.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(outfits);

        var dto = await Sut().Handle(new GetWearStatisticsQuery(_userId), CancellationToken.None);

        Assert.Empty(dto.TopWornItems);
        Assert.Equal(5, dto.TotalWearEvents);
    }

    [Fact]
    public async Task Handle_Throws_ForInvalidRange()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => Sut().Handle(new GetWearStatisticsQuery(_userId, "bogus"), CancellationToken.None));
    }
}
