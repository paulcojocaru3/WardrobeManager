using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.PlannedOutfits.Commands;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public sealed class PlannerCommandHandlerTests
{
    private readonly IPlannerEventRepository _planner = Substitute.For<IPlannerEventRepository>();
    private readonly IOutfitRepository _outfits = Substitute.For<IOutfitRepository>();
    private readonly IWeatherService _weather = Substitute.For<IWeatherService>();
    private readonly Guid _userId = Guid.NewGuid();

    private PlannerEvent OwnedEvent(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(), UserId = _userId, Name = "Trip", Type = "Vacation", Location = "Rome",
        StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(2),
    };

    // ---- Create ----
    [Fact]
    public async Task Create_PersistsEvent_WithActiveStatus()
    {
        var sut = new CreatePlannerEventCommandHandler(_planner);
        var cmd = new CreatePlannerEventCommand(_userId, "Trip", "Vacation", "Rome",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1), new List<string>());

        var id = await sut.Handle(cmd, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        await _planner.Received(1).AddAsync(Arg.Is<PlannerEvent>(p => p.Status == "Active"), Arg.Any<CancellationToken>());
    }

    // ---- Update ----
    [Fact]
    public async Task Update_ModifiesOwnedEvent()
    {
        var ev = OwnedEvent();
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        var sut = new UpdatePlannerEventCommandHandler(_planner);
        var cmd = new UpdatePlannerEventCommand(_userId, ev.Id, "Renamed", "Wedding", "Paris",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1), new List<string> { "Formal" });

        Assert.True(await sut.Handle(cmd, CancellationToken.None));
        Assert.Equal("Renamed", ev.Name);
        await _planner.Received(1).UpdateAsync(ev, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ReturnsFalse_WhenNotOwned()
    {
        var ev = new PlannerEvent { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "x", Type = "y", Location = "z" };
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        var sut = new UpdatePlannerEventCommandHandler(_planner);
        var cmd = new UpdatePlannerEventCommand(_userId, ev.Id, "n", "t", "l",
            DateTime.UtcNow.Date, DateTime.UtcNow.Date, new List<string>());

        Assert.False(await sut.Handle(cmd, CancellationToken.None));
    }

    // ---- Archive ----
    [Fact]
    public async Task Archive_SetsArchivedStatus()
    {
        var ev = OwnedEvent();
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        var sut = new ArchivePlannerEventCommandHandler(_planner);

        Assert.True(await sut.Handle(new ArchivePlannerEventCommand(_userId, ev.Id), CancellationToken.None));
        Assert.Equal("Archived", ev.Status);
    }

    [Fact]
    public async Task Archive_ReturnsFalse_WhenMissing()
    {
        _planner.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PlannerEvent?)null);
        var sut = new ArchivePlannerEventCommandHandler(_planner);
        Assert.False(await sut.Handle(new ArchivePlannerEventCommand(_userId, Guid.NewGuid()), CancellationToken.None));
    }

    // ---- Delete ----
    [Fact]
    public async Task Delete_RemovesOwnedEvent()
    {
        var ev = OwnedEvent();
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        var sut = new DeletePlannerEventCommandHandler(_planner);

        Assert.True(await sut.Handle(new DeletePlannerEventCommand(_userId, ev.Id), CancellationToken.None));
        await _planner.Received(1).DeleteAsync(ev, Arg.Any<CancellationToken>());
    }

    // ---- Add itinerary ----
    private AddEventItineraryCommandHandler AddSut()
        => new(_planner, _outfits, _weather, NullLogger<AddEventItineraryCommandHandler>.Instance);

    [Fact]
    public async Task AddItinerary_StoresForecastTemperature()
    {
        var ev = OwnedEvent();
        var outfit = new Outfit { Id = Guid.NewGuid(), UserId = _userId };
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _outfits.GetByIdAsync(outfit.Id, Arg.Any<CancellationToken>()).Returns(outfit);
        _weather.GetForecastAsync("Rome", Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<DailyForecast> { new(ev.StartDate, 18f, "Clear", "Spring") });

        var id = await AddSut().Handle(new AddEventItineraryCommand(_userId, ev.Id, outfit.Id, ev.StartDate, "Morning"), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        await _planner.Received(1).AddItineraryAsync(Arg.Any<EventItinerary>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddItinerary_SwallowsForecastFailure()
    {
        var ev = OwnedEvent();
        var outfit = new Outfit { Id = Guid.NewGuid(), UserId = _userId };
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _outfits.GetByIdAsync(outfit.Id, Arg.Any<CancellationToken>()).Returns(outfit);
        _weather.GetForecastAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns<List<DailyForecast>>(_ => throw new InvalidOperationException("no key"));

        var id = await AddSut().Handle(new AddEventItineraryCommand(_userId, ev.Id, outfit.Id, ev.StartDate, "Morning"), CancellationToken.None);
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task AddItinerary_Throws_WhenPlannerEventMissing()
    {
        _planner.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PlannerEvent?)null);
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => AddSut().Handle(new AddEventItineraryCommand(_userId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "M"), CancellationToken.None));
    }

    [Fact]
    public async Task AddItinerary_Throws_WhenOutfitMissing()
    {
        var ev = OwnedEvent();
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _outfits.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Outfit?)null);
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => AddSut().Handle(new AddEventItineraryCommand(_userId, ev.Id, Guid.NewGuid(), DateTime.UtcNow, "M"), CancellationToken.None));
    }

    // ---- Update itinerary ----
    [Fact]
    public async Task UpdateItinerary_ModifiesExisting()
    {
        var outfit = new Outfit { Id = Guid.NewGuid(), UserId = _userId };
        var itinerary = new EventItinerary { Id = Guid.NewGuid(), Date = DateTime.UtcNow.Date, Moment = "AM" };
        var ev = OwnedEvent();
        ev.Itineraries.Add(itinerary);
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _outfits.GetByIdAsync(outfit.Id, Arg.Any<CancellationToken>()).Returns(outfit);
        var sut = new UpdateEventItineraryCommandHandler(_planner, _outfits, _weather, NullLogger<UpdateEventItineraryCommandHandler>.Instance);

        var cmd = new UpdateEventItineraryCommand(_userId, ev.Id, itinerary.Id, outfit.Id, itinerary.Date, "PM");
        Assert.True(await sut.Handle(cmd, CancellationToken.None));
        Assert.Equal("PM", itinerary.Moment);
        await _planner.Received(1).UpdateItineraryAsync(itinerary, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateItinerary_ReturnsFalse_WhenItineraryMissing()
    {
        var outfit = new Outfit { Id = Guid.NewGuid(), UserId = _userId };
        var ev = OwnedEvent(); // no itineraries
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _outfits.GetByIdAsync(outfit.Id, Arg.Any<CancellationToken>()).Returns(outfit);
        var sut = new UpdateEventItineraryCommandHandler(_planner, _outfits, _weather, NullLogger<UpdateEventItineraryCommandHandler>.Instance);

        var cmd = new UpdateEventItineraryCommand(_userId, ev.Id, Guid.NewGuid(), outfit.Id, DateTime.UtcNow.Date, "PM");
        Assert.False(await sut.Handle(cmd, CancellationToken.None));
    }

    // ---- Delete itinerary ----
    [Fact]
    public async Task DeleteItinerary_RemovesExisting()
    {
        var ev = OwnedEvent();
        var itinerary = new EventItinerary { Id = Guid.NewGuid(), PlannerEventId = ev.Id };
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _planner.GetItineraryByIdAsync(itinerary.Id, Arg.Any<CancellationToken>()).Returns(itinerary);
        var sut = new DeleteEventItineraryCommandHandler(_planner);

        Assert.True(await sut.Handle(new DeleteEventItineraryCommand(_userId, ev.Id, itinerary.Id), CancellationToken.None));
        await _planner.Received(1).DeleteItineraryAsync(itinerary, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteItinerary_ReturnsFalse_WhenMissing()
    {
        var ev = OwnedEvent();
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _planner.GetItineraryByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((EventItinerary?)null);
        var sut = new DeleteEventItineraryCommandHandler(_planner);

        Assert.False(await sut.Handle(new DeleteEventItineraryCommand(_userId, ev.Id, Guid.NewGuid()), CancellationToken.None));
    }
}
