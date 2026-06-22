using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.PlannedOutfits.Commands;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Tests.Unit.Planning;

[Trait("Category", "Unit")]
public sealed class CheckWeatherAlertsHandlerTests
{
    private readonly IPlannerEventRepository _planner = Substitute.For<IPlannerEventRepository>();
    private readonly IWeatherService _weather = Substitute.For<IWeatherService>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private CheckWeatherAlertsCommandHandler Sut()
        => new(_planner, _weather, _dispatcher, NullLogger<CheckWeatherAlertsCommandHandler>.Instance,
               new FakeClock(Now));

    private PlannerEvent EventWithItinerary(float storedTemp)
    {
        var date = Now.Date;
        var ev = new PlannerEvent
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Trip",
            Location = "Rome",
            StartDate = date,
            EndDate = date,
        };
        ev.Itineraries.Add(new EventItinerary { Id = Guid.NewGuid(), Date = date, StoredTemperature = storedTemp });
        return ev;
    }

    private void StubEvents(params PlannerEvent[] events)
        => _planner.GetActiveWithUpcomingItinerariesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                   .Returns(events.ToList());

    private void StubForecast(float temp)
        => _weather.GetForecastAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
                   .Returns(new List<DailyForecast> { new(Now.Date, temp, "Clear", "Summer") });

    [Fact]
    public async Task Handle_ReturnsZero_WhenNoEvents()
    {
        StubEvents();

        Assert.Equal(0, await Sut().Handle(new CheckWeatherAlertsCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DispatchesAlert_OnSignificantTemperatureDrift()
    {
        StubEvents(EventWithItinerary(storedTemp: 15f));
        StubForecast(temp: 28f); // 13C warmer -> significant

        var sent = await Sut().Handle(new CheckWeatherAlertsCommand(), CancellationToken.None);

        Assert.Equal(1, sent);
        await _dispatcher.Received(1).DispatchAsync(
            Arg.Any<Guid>(), "WeatherAlert", Arg.Any<string>(),
            Arg.Is<string>(m => m.Contains("warmer")), Arg.Any<object?>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotDispatch_OnMinorDrift()
    {
        StubEvents(EventWithItinerary(storedTemp: 20f));
        StubForecast(temp: 22f); // 2C -> below threshold

        var sent = await Sut().Handle(new CheckWeatherAlertsCommand(), CancellationToken.None);

        Assert.Equal(0, sent);
        await _dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<object?>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SwallowsForecastFailure_AndReportsNoAlert()
    {
        StubEvents(EventWithItinerary(storedTemp: 15f));
        _weather.GetForecastAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
                .Returns<List<DailyForecast>>(_ => throw new InvalidOperationException("no key"));

        Assert.Equal(0, await Sut().Handle(new CheckWeatherAlertsCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Skips_WhenForecastHasNoMatchingDay()
    {
        StubEvents(EventWithItinerary(storedTemp: 15f));
        _weather.GetForecastAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
                .Returns(new List<DailyForecast> { new(Now.Date.AddDays(10), 30f, "Clear", "Summer") });

        Assert.Equal(0, await Sut().Handle(new CheckWeatherAlertsCommand(), CancellationToken.None));
    }

    private sealed class FakeClock(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
