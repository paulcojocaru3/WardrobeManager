using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.PlannedOutfits;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Tests.Unit.Planning;

[Trait("Category", "Unit")]
public sealed class EventOutfitPlanningServiceTests
{
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private readonly Guid _userId = Guid.NewGuid();

    private EventOutfitPlanningService Sut() => new(_clothing);

    private static ClothingItem Item(string? usage = null, string? season = null)
        => new() { Id = Guid.NewGuid(), Usage = usage, Season = season };

    // ---- ResolveDayPlan: keyword inference from an existing moment ----
    [Theory]
    [InlineData("Hit the gym", "Sports")]
    [InlineData("Morning run", "Sports")]
    [InlineData("Catch a flight", "Travel")]
    [InlineData("Dinner downtown", "Party")]
    [InlineData("Wedding ceremony", "Formal")]
    [InlineData("Team meeting", "Smart Casual")]   // Business -> canonicalized
    [InlineData("Romantic date", "Smart Casual")]  // Date -> canonicalized
    [InlineData("City walk", "Smart Casual")]
    public void ResolveDayPlan_InfersStyle_FromExistingMoment(string moment, string expectedStyle)
    {
        var (style, returnedMoment) = Sut().ResolveDayPlan("Vacation", 3, weather: null, existingMoment: moment);

        Assert.Equal(expectedStyle, style);
        Assert.Equal(moment, returnedMoment); // the user's moment is preserved verbatim
    }

    [Fact]
    public void ResolveDayPlan_UnknownMoment_FallsBackToEventDefault()
    {
        var (style, moment) = Sut().ResolveDayPlan("Wedding", 2, weather: null, existingMoment: "Something else");

        Assert.Equal("Formal", style); // Wedding default
        Assert.Equal("Something else", moment);
    }

    // ---- ResolveDayPlan: first-day templates ----
    [Theory]
    [InlineData("Vacation", "Travel", "Travel")]
    [InlineData("Business Trip", "Smart Casual", "Business")] // Business canonicalized
    [InlineData("Wedding", "Formal", "Ceremony")]
    [InlineData("Party", "Party", "Evening")]
    [InlineData("Date", "Smart Casual", "Evening")]
    [InlineData("Meeting", "Smart Casual", "Meeting")]
    [InlineData("Weekend", "Casual", "Leisure")]
    [InlineData("Unrecognized", "Casual", "Day")]
    public void ResolveDayPlan_Day0_UsesEventTemplate(string type, string expectedStyle, string expectedMoment)
    {
        var (style, moment) = Sut().ResolveDayPlan(type, 0, weather: null);

        Assert.Equal(expectedStyle, style);
        Assert.Equal(expectedMoment, moment);
    }

    // ---- ResolveDayPlan: later days use preferred style / weather ----
    [Fact]
    public void ResolveDayPlan_LaterDay_PrefersUserStyle()
    {
        var (style, _) = Sut().ResolveDayPlan("Vacation", 2, weather: null,
            existingMoment: null, preferredStyles: new List<string> { "Sporty", "Chic" });

        Assert.Equal("Sporty", style);
    }

    [Fact]
    public void ResolveDayPlan_LaterDay_UsesSeasonWhenNoTemplate()
    {
        var (style, moment) = Sut().ResolveDayPlan("Vacation", 2, new WeatherData(20f, "Clear", "Spring"));

        Assert.Equal("Spring", style); // Vacation default -> season suggestion
        Assert.Equal("Day", moment);   // mild temperature
    }

    [Theory]
    [InlineData("rain", 15f, "Indoor")]
    [InlineData("storm", 15f, "Indoor")]
    [InlineData("Clear", 30f, "Outdoor")]
    [InlineData("Clear", 5f, "Indoor")]
    [InlineData("Clear", 18f, "Day")]
    public void ResolveDayPlan_LaterDay_DerivesMomentFromWeather(string condition, float temp, string expectedMoment)
    {
        var (_, moment) = Sut().ResolveDayPlan("Vacation", 1, new WeatherData(temp, condition, "Spring"));

        Assert.Equal(expectedMoment, moment);
    }

    // ---- SelectStartItemAsync ----
    [Fact]
    public async Task SelectStart_ReturnsNull_WhenWardrobeEmpty()
    {
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<ClothingItem>());

        var result = await Sut().SelectStartItemAsync(_userId, "Casual", weather: null, excludedItemIds: null, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SelectStart_PrefersUsageAndSeasonMatch()
    {
        var perfect = Item(usage: "Casual", season: "Summer");
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new List<ClothingItem> { Item(usage: "Formal", season: "Winter"), perfect });

        var result = await Sut().SelectStartItemAsync(_userId, "Casual", new WeatherData(28f, "Clear", "Summer"), null, CancellationToken.None);

        Assert.Same(perfect, result);
    }

    [Fact]
    public async Task SelectStart_FallsBackToSeasonMatch()
    {
        var seasonOnly = Item(usage: "Formal", season: "Summer");
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<ClothingItem> { seasonOnly });

        var result = await Sut().SelectStartItemAsync(_userId, "Casual", new WeatherData(28f, "Clear", "Summer"), null, CancellationToken.None);

        Assert.Same(seasonOnly, result);
    }

    [Fact]
    public async Task SelectStart_FallsBackToStyleMatch()
    {
        var styleOnly = Item(usage: "Casual", season: "Winter");
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<ClothingItem> { styleOnly });

        var result = await Sut().SelectStartItemAsync(_userId, "Casual", new WeatherData(28f, "Clear", "Summer"), null, CancellationToken.None);

        Assert.Same(styleOnly, result);
    }

    [Fact]
    public async Task SelectStart_FallsBackToAnyItem_WhenNothingMatches()
    {
        var any = Item(usage: "Formal", season: "Winter");
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<ClothingItem> { any });

        var result = await Sut().SelectStartItemAsync(_userId, "Casual", new WeatherData(28f, "Clear", "Summer"), null, CancellationToken.None);

        Assert.Same(any, result);
    }

    [Fact]
    public async Task SelectStart_RespectsExclusion_WhenOthersAvailable()
    {
        var excluded = Item(usage: "Casual", season: "Summer");
        var keep = Item(usage: "Casual", season: "Summer");
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<ClothingItem> { excluded, keep });

        var result = await Sut().SelectStartItemAsync(_userId, "Casual", new WeatherData(28f, "Clear", "Summer"), new[] { excluded.Id }, CancellationToken.None);

        Assert.Same(keep, result);
    }

    [Fact]
    public async Task SelectStart_FallsBackToFullList_WhenAllExcluded()
    {
        var only = Item(usage: "Casual", season: "Summer");
        _clothing.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<ClothingItem> { only });

        // excluding the only item empties the pool, so the service falls back to the full wardrobe
        var result = await Sut().SelectStartItemAsync(_userId, "Casual", new WeatherData(28f, "Clear", "Summer"), new[] { only.Id }, CancellationToken.None);

        Assert.Same(only, result);
    }

    // ---- CompareForecastToCurrentWeather (static) ----
    [Fact]
    public void CompareForecast_NotSignificant_WhenEitherNull()
    {
        var ok = new WeatherData(20f, "Clear", "Spring");

        Assert.False(EventOutfitPlanningService.CompareForecastToCurrentWeather(null, ok).IsSignificantChange);
        Assert.False(EventOutfitPlanningService.CompareForecastToCurrentWeather(ok, null).IsSignificantChange);
    }

    [Fact]
    public void CompareForecast_NotSignificant_WhenTemperatureNotFinite()
    {
        var nan = new WeatherData(float.NaN, "Clear", "Spring");
        var ok = new WeatherData(20f, "Clear", "Spring");

        Assert.False(EventOutfitPlanningService.CompareForecastToCurrentWeather(nan, ok).IsSignificantChange);
    }

    [Fact]
    public void CompareForecast_FlagsChange_AboveThreshold()
    {
        var (significant, delta) = EventOutfitPlanningService.CompareForecastToCurrentWeather(
            new WeatherData(10f, "Clear", "Spring"),
            new WeatherData(18f, "Clear", "Spring"));

        Assert.True(significant);
        Assert.Equal(8f, delta);
    }

    [Fact]
    public void CompareForecast_NotSignificant_BelowThreshold()
    {
        var (significant, delta) = EventOutfitPlanningService.CompareForecastToCurrentWeather(
            new WeatherData(20f, "Clear", "Spring"),
            new WeatherData(22f, "Clear", "Spring"));

        Assert.False(significant);
        Assert.Equal(2f, delta);
    }
}
