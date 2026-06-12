using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Commands;
using WardrobeManager.Application.Outfits.Learning;
using WardrobeManager.Application.PlannedOutfits.Queries;
using WardrobeManager.Application.Users.Commands;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public sealed class UpdateUserPreferencesCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private UpdateUserPreferencesCommandHandler Sut() => new(_users);

    [Fact]
    public async Task Handle_UpdatesPreferences_AndNormalizes()
    {
        var user = new User { Id = Guid.NewGuid() };
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var cmd = new UpdateUserPreferencesCommand(user.Id,
            FavoriteColors: new List<string> { "Blue", "blue", " Red " },
            PreferredCity: " Paris ", ThemePreference: "dark", OuterwearMode: "bogus", OuterwearTempThreshold: 99);

        var result = await Sut().Handle(cmd, CancellationToken.None);

        Assert.Equal(new[] { "blue", "red" }, user.FavoriteColors);  // lowercased + deduped + trimmed
        Assert.Equal("Paris", user.PreferredCity);
        Assert.Equal("auto", user.OuterwearMode);                    // invalid -> auto
        Assert.Equal(30, user.OuterwearTempThreshold);               // clamped to 30
        Assert.Equal("Paris", result.PreferredCity);
        await _users.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenUserNotFound()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        await Assert.ThrowsAsync<Exception>(
            () => Sut().Handle(new UpdateUserPreferencesCommand(Guid.NewGuid(), null, null, null), CancellationToken.None));
    }
}

[Trait("Category", "Unit")]
public sealed class RecordOutfitWearCommandHandlerTests
{
    private readonly IOutfitRepository _outfits = Substitute.For<IOutfitRepository>();
    private readonly IWearEventRepository _wear = Substitute.For<IWearEventRepository>();
    private RecordOutfitWearCommandHandler Sut() => new(_outfits, _wear);

    [Fact]
    public async Task Handle_RecordsWearEvents_ForOutfitItems()
    {
        var userId = Guid.NewGuid();
        var outfit = new Outfit
        {
            Id = Guid.NewGuid(), UserId = userId,
            Items = new() { new ClothingItem { Id = Guid.NewGuid() }, new ClothingItem { Id = Guid.NewGuid() } },
        };
        _wear.GetByUserIdAsync(userId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<WearEvent>());
        _outfits.GetByIdAsync(outfit.Id, Arg.Any<CancellationToken>()).Returns(outfit);

        var result = await Sut().Handle(new RecordOutfitWearCommand(userId, outfit.Id), CancellationToken.None);

        Assert.True(result);
        await _wear.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<WearEvent>>(e => e.Count() == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsFalse_WhenOutfitMissingOrNotOwned()
    {
        var userId = Guid.NewGuid();
        _wear.GetByUserIdAsync(userId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(new List<WearEvent>());
        _outfits.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new Outfit { UserId = Guid.NewGuid() });

        Assert.False(await Sut().Handle(new RecordOutfitWearCommand(userId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ReturnsFalse_WhenDailyLimitReached()
    {
        var userId = Guid.NewGuid();
        var sessions = Enumerable.Range(0, 10)
            .Select(i => new WearEvent { OutfitId = Guid.NewGuid(), WearDate = DateTime.UtcNow.AddMinutes(-i) })
            .ToList();
        _wear.GetByUserIdAsync(userId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(sessions);

        Assert.False(await Sut().Handle(new RecordOutfitWearCommand(userId, Guid.NewGuid()), CancellationToken.None));
    }
}

[Trait("Category", "Unit")]
public sealed class RecordOutfitFeedbackCommandHandlerTests
{
    private readonly IOutfitFeedbackRepository _feedback = Substitute.For<IOutfitFeedbackRepository>();
    private readonly IUserEvaluatorWeightsRepository _weights = Substitute.For<IUserEvaluatorWeightsRepository>();
    private readonly IWeightLearningService _learning = Substitute.For<IWeightLearningService>();

    private RecordOutfitFeedbackCommandHandler Sut()
        => new(_feedback, _weights, _learning, NullLogger<RecordOutfitFeedbackCommandHandler>.Instance);

    private static RecordOutfitFeedbackCommand Command()
        => new(Guid.NewGuid(), Guid.NewGuid(), new List<OutfitFeedbackItem>
        {
            new(Guid.NewGuid(), "Accepted"),
            new(Guid.NewGuid(), "Shown"),     // ignored
        });

    [Fact]
    public async Task Handle_RecordsActions_AndTriggersRetrain_OverThreshold()
    {
        _feedback.CountActionableAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(8);
        _weights.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserEvaluatorWeights?)null);

        var result = await Sut().Handle(Command(), CancellationToken.None);

        Assert.True(result);
        await _feedback.Received(1).RecordActionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), FeedbackAction.Accepted, Arg.Any<CancellationToken>());
        await _learning.Received(1).RetrainAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SkipsRetrain_BelowThreshold()
    {
        _feedback.CountActionableAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(3);
        _weights.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserEvaluatorWeights?)null);

        await Sut().Handle(Command(), CancellationToken.None);

        await _learning.DidNotReceive().RetrainAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SwallowsRetrainFailure()
    {
        _feedback.CountActionableAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(20);
        _weights.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserEvaluatorWeights?)null);
        _learning.RetrainAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new Exception("train fail"));

        Assert.True(await Sut().Handle(Command(), CancellationToken.None));
    }
}

[Trait("Category", "Unit")]
public sealed class PlannerEventQueryHandlerTests
{
    private readonly IPlannerEventRepository _planner = Substitute.For<IPlannerEventRepository>();
    private readonly IWeatherService _weather = Substitute.For<IWeatherService>();

    private GetPlannerEventsQueryHandler ActiveSut()
        => new(_planner, _weather, NullLogger<GetPlannerEventsQueryHandler>.Instance);

    [Fact]
    public async Task GetPlannerEvents_AutoArchivesPastActiveEvents()
    {
        var past = new PlannerEvent
        {
            Id = Guid.NewGuid(), Status = "Active", Name = "Old", Location = "X",
            StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(-5),
        };
        _planner.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new[] { past });

        var result = await ActiveSut().Handle(new GetPlannerEventsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(result.PlannerEvents); // archived -> filtered from active
        await _planner.Received(1).UpdateAsync(Arg.Is<PlannerEvent>(p => p.Status == "Archived"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPlannerEvents_RaisesWeatherAlert_OnTemperatureDrift()
    {
        var today = DateTime.UtcNow.Date;
        var outfit = new Outfit { Id = Guid.NewGuid(), Name = "Look", Items = new() };
        var ev = new PlannerEvent
        {
            Id = Guid.NewGuid(), Status = "Active", Name = "Trip", Location = "Rome",
            StartDate = today, EndDate = today.AddDays(2),
            Itineraries = new()
            {
                new EventItinerary { Id = Guid.NewGuid(), Date = today, StoredTemperature = 10f, Outfit = outfit, OutfitId = outfit.Id },
            },
        };
        _planner.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new[] { ev });
        _weather.GetForecastAsync("Rome", Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<DailyForecast> { new(today, 22f, "Sunny", "Summer") }); // +12 drift

        var result = await ActiveSut().Handle(new GetPlannerEventsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Single(result.PlannerEvents);
        Assert.NotNull(result.WeatherAlert);
        Assert.True(result.WeatherAlert!.IsSignificantChange);
    }

    [Fact]
    public async Task GetArchived_ReturnsArchived_AndDeletesStaleOnes()
    {
        var stale = new PlannerEvent { Id = Guid.NewGuid(), Status = "Archived", ArchivedAt = DateTime.UtcNow.AddDays(-40), Name = "Stale", Location = "X" };
        var recent = new PlannerEvent { Id = Guid.NewGuid(), Status = "Archived", ArchivedAt = DateTime.UtcNow.AddDays(-5), Name = "Recent", Location = "Y" };
        _planner.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new[] { stale, recent });

        var sut = new GetArchivedPlannerEventsQueryHandler(_planner);
        var result = (await sut.Handle(new GetArchivedPlannerEventsQuery(Guid.NewGuid()), CancellationToken.None)).ToList();

        Assert.Single(result);
        Assert.Equal("Recent", result[0].Name);
        await _planner.Received(1).DeleteAsync(Arg.Is<PlannerEvent>(p => p.Name == "Stale"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetArchived_MapsItinerariesWithOutfit()
    {
        var item = new ClothingItem { Id = Guid.NewGuid(), Name = "Top", Type = ClothingType.Top, ProcessedImageUrl = "img" };
        var outfit = new Outfit { Id = Guid.NewGuid(), Name = "Look", Items = new() { item } };
        var archived = new PlannerEvent
        {
            Id = Guid.NewGuid(), Status = "Archived", ArchivedAt = DateTime.UtcNow.AddDays(-3),
            Name = "Trip", Location = "Rome",
            Itineraries = new()
            {
                new EventItinerary { Id = Guid.NewGuid(), OutfitId = outfit.Id, Outfit = outfit, Date = DateTime.UtcNow, Moment = "Dinner" },
            },
        };
        _planner.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new[] { archived });

        var sut = new GetArchivedPlannerEventsQueryHandler(_planner);
        var result = (await sut.Handle(new GetArchivedPlannerEventsQuery(Guid.NewGuid()), CancellationToken.None)).ToList();

        Assert.Single(result);
        var itinerary = Assert.Single(result[0].Itineraries);
        Assert.Equal("Look", itinerary.Outfit.Name);
        Assert.Single(itinerary.Outfit.Items);
    }
}
