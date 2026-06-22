using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Feasibility;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Application.Outfits.Prompting;
using WardrobeManager.Application.PlannedOutfits.Commands;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Planning;

[Trait("Category", "Unit")]
public sealed class EventOutfitsHandlerTests
{
    private readonly IPlannerEventRepository _planner = Substitute.For<IPlannerEventRepository>();
    private readonly IOutfitRepository _outfits = Substitute.For<IOutfitRepository>();
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private readonly IOutfitGenerator _generator = Substitute.For<IOutfitGenerator>();
    private readonly IWeatherService _weather = Substitute.For<IWeatherService>();
    private readonly IEventOutfitPlanningService _planning = Substitute.For<IEventOutfitPlanningService>();
    private readonly IStartItemSelector _selector = Substitute.For<IStartItemSelector>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IOutfitStylist _stylist = Substitute.For<IOutfitStylist>();
    private readonly IItemPairScoreRepository _pairScores = Substitute.For<IItemPairScoreRepository>();
    private readonly IUserLearningProfileRepository _learningProfiles = Substitute.For<IUserLearningProfileRepository>();
    private readonly IMlService _ml = Substitute.For<IMlService>();
    private readonly IThermalRules _thermal = Substitute.For<IThermalRules>();
    private readonly IOccasionFormalityRules _occasion = Substitute.For<IOccasionFormalityRules>();
    private readonly IOutfitFeedbackRepository _feedback = Substitute.For<IOutfitFeedbackRepository>();
    private readonly Guid _userId = Guid.NewGuid();

    private StylistOutfitComposer Composer() => new(
        _stylist, _pairScores, _learningProfiles, NullLogger<StylistOutfitComposer>.Instance);
    private StylistCandidatePoolBuilder PoolBuilder() => new(_clothing, _ml, _thermal, NullLogger<StylistCandidatePoolBuilder>.Instance);

    private GenerateEventOutfitsCommandHandler GenerateSut() => new(
        _planner, _outfits, _clothing, _generator, _weather, _planning, _selector,
        _users, Composer(), PoolBuilder(), new StylistSettings(), _occasion, _feedback,
        NullLogger<GenerateEventOutfitsCommandHandler>.Instance);

    private RegenerateEventItineraryOutfitCommandHandler RegenerateSut() => new(
        _planner, _outfits, _generator, _clothing, _planning, _weather,
        _users, Composer(), PoolBuilder(), new StylistSettings(), _occasion, _feedback,
        NullLogger<RegenerateEventItineraryOutfitCommandHandler>.Instance);

    // ---- builders ----
    private PlannerEvent Event(int days = 2, Guid? owner = null)
    {
        var start = DateTime.UtcNow.Date;
        return new PlannerEvent
        {
            Id = Guid.NewGuid(),
            UserId = owner ?? _userId,
            Name = "Trip",
            Type = "Vacation",
            Location = "Rome",
            StartDate = start,
            EndDate = start.AddDays(days - 1),
            PreferredStyles = new List<string>(),
        };
    }

    private static ClothingItem WithEmbedding() => new() { Id = Guid.NewGuid(), Embedding = new[] { 0.1f, 0.2f } };

    private static List<ClothingItem> Wardrobe(int n)
    {
        var list = new List<ClothingItem>();
        for (var i = 0; i < n; i++)
        {
            list.Add(new ClothingItem { Id = Guid.NewGuid() });
        }
        return list;
    }

    private static EventItinerary Itinerary(DateTime date, Outfit? outfit = null)
        => new() { Id = Guid.NewGuid(), Date = date, Moment = "Day", Outfit = outfit };

    private void StubResolveDayPlan(string style = "Casual", string moment = "Day")
    {
        (string Style, string Moment) plan = (style, moment);
        _planning.ResolveDayPlan(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<WeatherData?>(), Arg.Any<string?>(), Arg.Any<List<string>?>())
                 .Returns(plan);
    }

    private void StubForecast(int days)
    {
        var start = DateTime.UtcNow.Date;
        var list = new List<DailyForecast>();
        for (var i = 0; i < days; i++)
        {
            list.Add(new DailyForecast(start.AddDays(i), 20f, "Clear", "Spring"));
        }
        _weather.GetForecastAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>()).Returns(list);
    }

    private void StubGeneratorReturnsItem()
    {
        var dto = new AiGeneratedOutfitDto { SelectedItems = new List<SimilarItemDto> { new() { Id = Guid.NewGuid() } } };
        _generator.GenerateAiOutfitAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<OutfitGenerationOptions>(), Arg.Any<CancellationToken>()).Returns(dto);
    }

    private void StubSelectorReturns(ClothingItem? item)
        => _selector.SelectAsync(Arg.Any<Guid>(), Arg.Any<PromptIntent>(), Arg.Any<IReadOnlyCollection<Guid>?>(), Arg.Any<WeatherData?>(), Arg.Any<CancellationToken>())
                    .Returns(item);

    private void StubPlanningSelectStartReturns(ClothingItem? item)
        => _planning.SelectStartItemAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<WeatherData?>(), Arg.Any<IReadOnlyCollection<Guid>?>(), Arg.Any<CancellationToken>())
                    .Returns(item);

    private void StubResolvedItems(int n)
        => _clothing.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
                    .Returns(n == 0 ? new List<ClothingItem>() : Wardrobe(n));

    // ================= GenerateEventOutfitsCommandHandler =================

    [Fact]
    public async Task Generate_Throws_WhenEventMissing()
    {
        _planner.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PlannerEvent?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => GenerateSut().Handle(new GenerateEventOutfitsCommand(_userId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Generate_Throws_WhenNotOwned()
    {
        var ev = Event(owner: Guid.NewGuid());
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => GenerateSut().Handle(new GenerateEventOutfitsCommand(_userId, ev.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Generate_Throws_WhenDateRangeTooLong()
    {
        var ev = Event(days: 40);
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => GenerateSut().Handle(new GenerateEventOutfitsCommand(_userId, ev.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Generate_Throws_WhenTooFewClothes()
    {
        var ev = Event();
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(Wardrobe(4));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => GenerateSut().Handle(new GenerateEventOutfitsCommand(_userId, ev.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Generate_CreatesOutfitsAndItineraries_OnHappyPath()
    {
        var ev = Event(days: 2);
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(Wardrobe(5));
        StubForecast(2);
        StubResolveDayPlan();
        StubSelectorReturns(WithEmbedding());
        StubGeneratorReturnsItem();
        StubResolvedItems(1);

        var result = await GenerateSut().Handle(new GenerateEventOutfitsCommand(_userId, ev.Id), CancellationToken.None);

        Assert.Equal(2, result.DaysProcessed);
        Assert.Equal(2, result.OutfitsCreated);
        await _outfits.Received(2).AddAsync(Arg.Any<Outfit>(), Arg.Any<CancellationToken>());
        await _planner.Received(2).AddItineraryAsync(Arg.Any<EventItinerary>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generate_SwallowsForecastFailure_AndStillGenerates()
    {
        var ev = Event(days: 1);
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(Wardrobe(5));
        _weather.GetForecastAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
                .Returns<List<DailyForecast>>(_ => throw new InvalidOperationException("no key"));
        StubResolveDayPlan();
        StubSelectorReturns(WithEmbedding());
        StubGeneratorReturnsItem();
        StubResolvedItems(1);

        var result = await GenerateSut().Handle(new GenerateEventOutfitsCommand(_userId, ev.Id), CancellationToken.None);

        Assert.Equal(1, result.OutfitsCreated);
    }

    [Fact]
    public async Task Generate_SkipsDay_WhenNoStartItemFound()
    {
        var ev = Event(days: 2);
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(Wardrobe(5));
        StubForecast(2);
        StubResolveDayPlan();
        StubSelectorReturns(null);
        StubPlanningSelectStartReturns(null);

        var result = await GenerateSut().Handle(new GenerateEventOutfitsCommand(_userId, ev.Id), CancellationToken.None);

        Assert.Equal(2, result.DaysProcessed);
        Assert.Equal(0, result.OutfitsCreated);
        await _outfits.DidNotReceive().AddAsync(Arg.Any<Outfit>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generate_DoesNotDuplicateExistingDay_AndUsesItForCooldown()
    {
        var ev = Event(days: 2);
        ev.ReuseAfterDays = 3;
        var existingTop = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top };
        ev.Itineraries.Add(Itinerary(ev.StartDate, new Outfit
        {
            Id = Guid.NewGuid(),
            Items = new List<ClothingItem> { existingTop }
        }));
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(Wardrobe(5));
        StubForecast(2);
        StubResolveDayPlan();
        StubSelectorReturns(WithEmbedding());
        StubGeneratorReturnsItem();
        StubResolvedItems(1);

        var result = await GenerateSut().Handle(
            new GenerateEventOutfitsCommand(_userId, ev.Id), CancellationToken.None);

        Assert.Equal(1, result.OutfitsCreated);
        await _planner.Received(1).AddItineraryAsync(Arg.Any<EventItinerary>(), Arg.Any<CancellationToken>());
        await _generator.Received(1).GenerateAiOutfitAsync(
            _userId,
            Arg.Any<Guid>(),
            Arg.Is<OutfitGenerationOptions>(options => options.ExcludedItemIds.Contains(existingTop.Id)),
            Arg.Any<CancellationToken>());
    }

    // ================= RegenerateEventItineraryOutfitCommandHandler =================

    [Fact]
    public async Task Regenerate_ReturnsFalse_WhenEventMissing()
    {
        _planner.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PlannerEvent?)null);

        Assert.False(await RegenerateSut().Handle(
            new RegenerateEventItineraryOutfitCommand(_userId, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Regenerate_ReturnsFalse_WhenNotOwned()
    {
        var ev = Event(owner: Guid.NewGuid());
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        Assert.False(await RegenerateSut().Handle(
            new RegenerateEventItineraryOutfitCommand(_userId, ev.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Regenerate_ReturnsFalse_WhenItineraryMissing()
    {
        var ev = Event();
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        Assert.False(await RegenerateSut().Handle(
            new RegenerateEventItineraryOutfitCommand(_userId, ev.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Regenerate_ReturnsFalse_WhenNoStartItem()
    {
        var ev = Event();
        var it = Itinerary(ev.StartDate);
        ev.Itineraries.Add(it);
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        StubForecast(2);
        StubResolveDayPlan();
        StubPlanningSelectStartReturns(null);

        Assert.False(await RegenerateSut().Handle(
            new RegenerateEventItineraryOutfitCommand(_userId, ev.Id, it.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Regenerate_ReturnsFalse_WhenNoItemsResolved()
    {
        var ev = Event();
        var it = Itinerary(ev.StartDate);
        ev.Itineraries.Add(it);
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        StubForecast(2);
        StubResolveDayPlan();
        StubPlanningSelectStartReturns(WithEmbedding());
        StubGeneratorReturnsItem();
        StubResolvedItems(0); // generator produced ids but none resolve to real items

        Assert.False(await RegenerateSut().Handle(
            new RegenerateEventItineraryOutfitCommand(_userId, ev.Id, it.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Regenerate_SavesNewOutfit_AndUpdatesItinerary_OnHappyPath()
    {
        var ev = Event();
        var oldOutfit = new Outfit { Id = Guid.NewGuid(), Items = new List<ClothingItem> { new() { Id = Guid.NewGuid() } } };
        var it = Itinerary(ev.StartDate, oldOutfit);
        ev.Itineraries.Add(it);
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        StubForecast(2);
        StubResolveDayPlan();
        StubPlanningSelectStartReturns(WithEmbedding());
        StubGeneratorReturnsItem();
        StubResolvedItems(1);

        var ok = await RegenerateSut().Handle(
            new RegenerateEventItineraryOutfitCommand(_userId, ev.Id, it.Id), CancellationToken.None);

        Assert.True(ok);
        await _outfits.Received(1).AddAsync(Arg.Any<Outfit>(), Arg.Any<CancellationToken>());
        await _planner.Received(1).UpdateItineraryAsync(it, Arg.Any<CancellationToken>());
        Assert.NotEqual(Guid.Empty, it.OutfitId);
    }

    [Fact]
    public async Task Regenerate_ClampsNegativeDayIndex_WhenItineraryBeforeStart()
    {
        var ev = Event();
        var it = Itinerary(ev.StartDate.AddDays(-3)); // before the trip start -> day index would be negative
        ev.Itineraries.Add(it);
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        StubForecast(2);
        StubResolveDayPlan();
        StubPlanningSelectStartReturns(WithEmbedding());
        StubGeneratorReturnsItem();
        StubResolvedItems(1);

        Assert.True(await RegenerateSut().Handle(
            new RegenerateEventItineraryOutfitCommand(_userId, ev.Id, it.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Regenerate_ExcludesNearbyTopFromEveryGeneratorSlot()
    {
        var ev = Event(days: 2);
        ev.ReuseAfterDays = 3;
        var nearbyTop = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top };
        ev.Itineraries.Add(Itinerary(ev.StartDate, new Outfit
        {
            Id = Guid.NewGuid(),
            Items = new List<ClothingItem> { nearbyTop }
        }));
        var target = Itinerary(ev.StartDate.AddDays(1));
        ev.Itineraries.Add(target);
        _planner.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        StubForecast(2);
        StubResolveDayPlan();
        StubPlanningSelectStartReturns(WithEmbedding());
        StubGeneratorReturnsItem();
        StubResolvedItems(1);

        Assert.True(await RegenerateSut().Handle(
            new RegenerateEventItineraryOutfitCommand(_userId, ev.Id, target.Id), CancellationToken.None));

        await _generator.Received(1).GenerateAiOutfitAsync(
            _userId,
            Arg.Any<Guid>(),
            Arg.Is<OutfitGenerationOptions>(options => options.ExcludedItemIds.Contains(nearbyTop.Id)),
            Arg.Any<CancellationToken>());
    }
}
