using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.PlannedOutfits.Queries;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Tests.Unit.Planning;

[Trait("Category", "Unit")]
public sealed class PlannerQueryHandlerTests
{
    private readonly IPlannerEventRepository _planner = Substitute.For<IPlannerEventRepository>();
    private readonly IWeatherService _weather = Substitute.For<IWeatherService>();
    private readonly Guid _userId = Guid.NewGuid();

    private GetPlannerEventsQueryHandler PlannerSut()
        => new(_planner, _weather, NullLogger<GetPlannerEventsQueryHandler>.Instance);

    private GetArchivedPlannerEventsQueryHandler ArchivedSut() => new(_planner);

    private static EventItinerary ItineraryWithOutfit(DateTime date, float? storedTemp = null) => new()
    {
        Id = Guid.NewGuid(),
        Date = date,
        Moment = "Day",
        StoredTemperature = storedTemp,
        Outfit = new Outfit { Id = Guid.NewGuid(), Name = "O", Items = new List<ClothingItem> { new() { Id = Guid.NewGuid(), Name = "Tee" } } },
    };

    private PlannerEvent ActiveEvent(DateTime start, DateTime end, params EventItinerary[] its)
    {
        var ev = new PlannerEvent
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Name = "Trip",
            Type = "Vacation",
            Location = "Rome",
            StartDate = start,
            EndDate = end,
            Status = "Active",
            PreferredStyles = new List<string>(),
        };
        ev.Itineraries.AddRange(its);
        return ev;
    }

    private void ReturnsEvents(params PlannerEvent[] events)
        => _planner.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(events.ToList());

    [Fact]
    public async Task Planner_ReturnsActiveEvents_MappedToDtos()
    {
        var today = DateTime.UtcNow.Date;
        var ev = ActiveEvent(today, today.AddDays(2), ItineraryWithOutfit(today.AddDays(1)));
        ReturnsEvents(ev);

        var result = await PlannerSut().Handle(new GetPlannerEventsQuery(_userId), CancellationToken.None);

        var dto = Assert.Single(result.PlannerEvents);
        Assert.Equal(ev.Id, dto.Id);
        Assert.Single(dto.Itineraries);
        Assert.Null(result.WeatherAlert); // no stored temperatures -> drift check skipped
    }

    [Fact]
    public async Task Planner_AutoArchivesPastActiveEvent()
    {
        var today = DateTime.UtcNow.Date;
        var ev = ActiveEvent(today.AddDays(-5), today.AddDays(-2)); // already ended
        ReturnsEvents(ev);

        var result = await PlannerSut().Handle(new GetPlannerEventsQuery(_userId), CancellationToken.None);

        Assert.Equal("Archived", ev.Status);
        await _planner.Received(1).UpdateAsync(ev, Arg.Any<CancellationToken>());
        Assert.Empty(result.PlannerEvents); // archived -> excluded from the active list
    }

    [Fact]
    public async Task Planner_AutoDeletesStaleArchivedEvent()
    {
        var today = DateTime.UtcNow.Date;
        var ev = ActiveEvent(today.AddDays(-60), today.AddDays(-50));
        ev.Status = "Archived";
        ev.ArchivedAt = DateTime.UtcNow.AddDays(-40); // archived more than 30 days ago
        ReturnsEvents(ev);

        await PlannerSut().Handle(new GetPlannerEventsQuery(_userId), CancellationToken.None);

        await _planner.Received(1).DeleteAsync(ev, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Planner_RaisesWeatherAlert_OnSignificantDrift()
    {
        var today = DateTime.UtcNow.Date;
        var ev = ActiveEvent(today, today.AddDays(2), ItineraryWithOutfit(today, storedTemp: 10f));
        ReturnsEvents(ev);
        _weather.GetForecastAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
                .Returns(new List<DailyForecast> { new(today, 20f, "Clear", "Summer") }); // delta 10 >= 5

        var result = await PlannerSut().Handle(new GetPlannerEventsQuery(_userId), CancellationToken.None);

        Assert.NotNull(result.WeatherAlert);
        Assert.Equal(ev.Id, result.WeatherAlert!.PlannerEventId);
    }

    [Fact]
    public async Task Planner_SwallowsForecastFailure_DuringDriftCheck()
    {
        var today = DateTime.UtcNow.Date;
        var ev = ActiveEvent(today, today.AddDays(2), ItineraryWithOutfit(today, storedTemp: 10f));
        ReturnsEvents(ev);
        _weather.GetForecastAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
                .Returns<List<DailyForecast>>(_ => throw new InvalidOperationException("no key"));

        var result = await PlannerSut().Handle(new GetPlannerEventsQuery(_userId), CancellationToken.None);

        Assert.Null(result.WeatherAlert); // failure swallowed; no alert, no throw
    }

    // ---- GetArchivedPlannerEventsQuery ----
    [Fact]
    public async Task Archived_ReturnsArchivedEvents()
    {
        var today = DateTime.UtcNow.Date;
        var ev = ActiveEvent(today.AddDays(-10), today.AddDays(-8), ItineraryWithOutfit(today.AddDays(-9)));
        ev.Status = "Archived";
        ev.ArchivedAt = DateTime.UtcNow.AddDays(-2); // recent -> not auto-deleted
        ReturnsEvents(ev);

        var result = await ArchivedSut().Handle(new GetArchivedPlannerEventsQuery(_userId), CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(ev.Id, dto.Id);
    }
}
