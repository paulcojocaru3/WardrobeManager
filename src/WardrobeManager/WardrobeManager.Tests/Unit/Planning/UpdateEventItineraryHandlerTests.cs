using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.PlannedOutfits.Commands;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Tests.Unit.Planning;

[Trait("Category", "Unit")]
public sealed class UpdateEventItineraryHandlerTests
{
    private readonly IPlannerEventRepository _planner = Substitute.For<IPlannerEventRepository>();
    private readonly IOutfitRepository _outfits = Substitute.For<IOutfitRepository>();
    private readonly IWeatherService _weather = Substitute.For<IWeatherService>();
    private readonly Guid _userId = Guid.NewGuid();

    private UpdateEventItineraryCommandHandler Sut()
        => new(_planner, _outfits, _weather, NullLogger<UpdateEventItineraryCommandHandler>.Instance);

    private PlannerEvent EventWith(EventItinerary it)
    {
        var start = DateTime.UtcNow.Date;
        var ev = new PlannerEvent
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Name = "Trip",
            Type = "Vacation",
            Location = "Rome",
            StartDate = start,
            EndDate = start.AddDays(3),
        };
        ev.Itineraries.Add(it);
        return ev;
    }

    [Fact]
    public async Task ReturnsFalse_WhenPlannerEventMissing()
    {
        _planner.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PlannerEvent?)null);

        var cmd = new UpdateEventItineraryCommand(_userId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.Date, "Day");

        Assert.False(await Sut().Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task ReturnsFalse_WhenOutfitNotOwned()
    {
        var it = new EventItinerary { Id = Guid.NewGuid(), Date = DateTime.UtcNow.Date };
        var ev = EventWith(it);
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _outfits.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(new Outfit { Id = Guid.NewGuid(), UserId = Guid.NewGuid() }); // belongs to someone else

        var cmd = new UpdateEventItineraryCommand(_userId, ev.Id, it.Id, Guid.NewGuid(), it.Date, "Day");

        Assert.False(await Sut().Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task UpdatesStoredTemperature_WhenDateChanges()
    {
        var oldDate = DateTime.UtcNow.Date;
        var newDate = oldDate.AddDays(1);
        var it = new EventItinerary { Id = Guid.NewGuid(), Date = oldDate, StoredTemperature = 10f };
        var ev = EventWith(it);
        var outfit = new Outfit { Id = Guid.NewGuid(), UserId = _userId };
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _outfits.GetByIdAsync(outfit.Id, Arg.Any<CancellationToken>()).Returns(outfit);
        _weather.GetForecastAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
                .Returns(new List<DailyForecast> { new(newDate, 25f, "Clear", "Summer") });

        var cmd = new UpdateEventItineraryCommand(_userId, ev.Id, it.Id, outfit.Id, newDate, "Evening");

        Assert.True(await Sut().Handle(cmd, CancellationToken.None));
        Assert.Equal(25f, it.StoredTemperature);
        Assert.Equal("Evening", it.Moment);
        await _planner.Received(1).UpdateItineraryAsync(it, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task KeepsStoredTemperature_WhenForecastFails()
    {
        var oldDate = DateTime.UtcNow.Date;
        var newDate = oldDate.AddDays(1);
        var it = new EventItinerary { Id = Guid.NewGuid(), Date = oldDate, StoredTemperature = 10f };
        var ev = EventWith(it);
        var outfit = new Outfit { Id = Guid.NewGuid(), UserId = _userId };
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _outfits.GetByIdAsync(outfit.Id, Arg.Any<CancellationToken>()).Returns(outfit);
        _weather.GetForecastAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
                .Returns<List<DailyForecast>>(_ => throw new InvalidOperationException("no key"));

        var cmd = new UpdateEventItineraryCommand(_userId, ev.Id, it.Id, outfit.Id, newDate, "Evening");

        Assert.True(await Sut().Handle(cmd, CancellationToken.None));
        Assert.Equal(10f, it.StoredTemperature); // forecast failed -> stored temp preserved
    }
}
