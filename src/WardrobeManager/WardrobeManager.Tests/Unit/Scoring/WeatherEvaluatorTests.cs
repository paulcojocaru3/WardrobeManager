using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Enums;
using WardrobeManager.Tests.Unit.TestSupport;

namespace WardrobeManager.Tests.Unit.Scoring;

[Trait("Category", "Unit")]
public sealed class WeatherEvaluatorTests
{
    private readonly WeatherEvaluator _sut = new(Defaults.Thermal);

    private static OutfitGenerationContext Context(float temp, string condition = "Clear", string season = "Summer")
        => new() { Weather = new WeatherData(temp, condition, season) };

    [Fact]
    public void Metadata_IsStable()
    {
        Assert.Equal("Weather", _sut.Name);
    }

    [Fact]
    public void Evaluate_Abstains_WhenNoWeather()
    {
        var result = _sut.Evaluate(TestData.Item(season: "Summer"), new OutfitGenerationContext());
        Assert.Equal(1.0, result, 3);
    }

    [Fact]
    public void Evaluate_DoesNotVeto_WarmOnlyGarment_VetoesAreFeasibilityNow()
    {
        // the warm-only-when-freezing veto moved to IGarmentFeasibility; the soft evaluator only grades
        var shorts = TestData.Item(ClothingType.Bottom, subType: "shorts");
        var result = _sut.Evaluate(shorts, Context(temp: 5, season: "Summer"));
        Assert.True(result > 0.05);
    }

    [Fact]
    public void Evaluate_SeasonMatch_GetsBonus()
    {
        // base 0.5 + season match 0.5 = 1.0
        var item = TestData.Item(season: "Summer");
        var result = _sut.Evaluate(item, Context(temp: 25, season: "Summer"));
        Assert.Equal(1.5, result, 3);
    }

    [Fact]
    public void Evaluate_AllSeasonsItem_AlwaysGetsMatchBonus()
    {
        var item = TestData.Item(season: "All Seasons");
        var result = _sut.Evaluate(item, Context(temp: 2, season: "Winter"));
        Assert.Equal(1.5, result, 3);
    }

    [Fact]
    public void Evaluate_SummerTopInCold_IsLightlyPenalizedForLayering()
    {
        // temp < 15, summer Top -> -0.1; base 0.5 -> 0.4
        var item = TestData.Item(ClothingType.Top, season: "Summer");
        var result = _sut.Evaluate(item, Context(temp: 10, season: "Winter"));
        Assert.Equal(1.065, result, 3);
    }

    [Fact]
    public void Evaluate_SummerBottomInCold_IsHeavilyPenalized()
    {
        // temp < 15, summer Bottom -> -0.8; base 0.5 -> -0.3
        var item = TestData.Item(ClothingType.Bottom, season: "Summer");
        var result = _sut.Evaluate(item, Context(temp: 10, season: "Winter"));
        Assert.Equal(0.558, result, 3);
    }

    [Fact]
    public void Evaluate_WinterItem_WhenHot_IsHeavilyPenalized()
    {
        // the hard veto moved to IGarmentFeasibility; the soft score is a strong penalty (base 0.5 - 0.8).
        var item = TestData.Item(season: "Winter");
        var result = _sut.Evaluate(item, Context(temp: 25, season: "Summer"));
        Assert.Equal(0.558, result, 3);
    }

    [Fact]
    public void Evaluate_WrongSeason_GetsGeneralPenalty()
    {
        // temp 18 (not <15, not >22), season Winter, suggestion Spring -> -0.2; base 0.5 -> 0.3
        var item = TestData.Item(season: "Winter");
        var result = _sut.Evaluate(item, Context(temp: 18, season: "Spring"));
        Assert.Equal(0.9925, result, 4);
    }

    // season "" keeps the base at 0.5 so the hot-weather light boost is isolated.
    private static OutfitGenerationContext HotContext(bool preferLight = true, float temp = 32)
        => new() { Weather = new WeatherData(temp, "Clear", "Summer"), PreferLightOnHotDays = preferLight };

    [Fact]
    public void Evaluate_HotDay_BoostsShortsAndShortSleeveTops_AtFullStrength()
    {
        // temp 32 == HotFullC -> warmth 1 -> +0.4 boost; base 0.5 -> 0.9.
        var shorts = TestData.Item(ClothingType.Bottom, subType: "shorts", season: "");
        var tee = TestData.Item(ClothingType.Top, subType: "tshirts", season: "");

        Assert.Equal(1.428, _sut.Evaluate(shorts, HotContext()), 3);
        Assert.Equal(1.428, _sut.Evaluate(tee, HotContext()), 3);
    }

    [Fact]
    public void Evaluate_HotDay_DoesNotBoostLongBottoms()
    {
        var jeans = TestData.Item(ClothingType.Bottom, subType: "jeans", season: "");
        Assert.Equal(1.138, _sut.Evaluate(jeans, HotContext()), 3);
    }

    [Fact]
    public void Evaluate_LightBoost_ScalesGraduallyWithTemperature()
    {
        // temp 27 -> warmth (27-22)/(32-22)=0.5 -> +0.2; base 0.5 -> 0.7.
        var shorts = TestData.Item(ClothingType.Bottom, subType: "shorts", season: "");
        Assert.Equal(1.2825, _sut.Evaluate(shorts, HotContext(temp: 27)), 4);
    }

    [Fact]
    public void Evaluate_LightBoost_Disabled_WhenPreferenceOff()
    {
        var shorts = TestData.Item(ClothingType.Bottom, subType: "shorts", season: "");
        Assert.Equal(1.138, _sut.Evaluate(shorts, HotContext(preferLight: false)), 3);
    }

    [Fact]
    public void Evaluate_LightBoost_Inactive_AtOrBelowHotThreshold()
    {
        // 22°C is not above HotC (22) -> no boost.
        var shorts = TestData.Item(ClothingType.Bottom, subType: "shorts", season: "");
        Assert.Equal(1.138, _sut.Evaluate(shorts, HotContext(temp: 22)), 3);
    }

    [Fact]
    public void Evaluate_RainGear_GetsBoost()
    {
        // empty season -> stays at base 0.5; rain outerwear named "rain" -> +0.4 = 0.9
        var item = TestData.Item(ClothingType.Outerwear, season: "", name: "rain jacket");
        var result = _sut.Evaluate(item, Context(temp: 15, condition: "Rain", season: "Autumn"));
        Assert.Equal(1.428, result, 3);
    }
}
