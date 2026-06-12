using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Scoring;

[Trait("Category", "Unit")]
public sealed class WeatherEvaluatorTests
{
    private readonly WeatherEvaluator _sut = new();

    private static OutfitGenerationContext Context(float temp, string condition = "Clear", string season = "Summer")
        => new() { Weather = new WeatherData(temp, condition, season) };

    [Fact]
    public void Metadata_IsStable()
    {
        Assert.Equal("Weather", _sut.Name);
        Assert.Equal(0.40, _sut.Weight);
    }

    [Fact]
    public void Evaluate_Abstains_WhenNoWeather()
    {
        var result = _sut.Evaluate(TestData.Item(season: "Summer"), new OutfitGenerationContext());
        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_Vetoes_WarmOnlyGarment_WhenFreezing()
    {
        var shorts = TestData.Item(ClothingType.Bottom, subType: "shorts");
        var result = _sut.Evaluate(shorts, Context(temp: 5));
        Assert.Equal(-1.0, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_SeasonMatch_GetsBonus()
    {
        // base 0.5 + season match 0.5 = 1.0
        var item = TestData.Item(season: "Summer");
        var result = _sut.Evaluate(item, Context(temp: 25, season: "Summer"));
        Assert.Equal(1.0, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_AllSeasonsItem_AlwaysGetsMatchBonus()
    {
        var item = TestData.Item(season: "All Seasons");
        var result = _sut.Evaluate(item, Context(temp: 2, season: "Winter"));
        Assert.Equal(1.0, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_SummerTopInCold_IsLightlyPenalizedForLayering()
    {
        // temp < 15, summer Top -> -0.1; base 0.5 -> 0.4
        var item = TestData.Item(ClothingType.Top, season: "Summer");
        var result = _sut.Evaluate(item, Context(temp: 10, season: "Winter"));
        Assert.Equal(0.4, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_SummerBottomInCold_IsHeavilyPenalized()
    {
        // temp < 15, summer Bottom -> -0.8; base 0.5 -> -0.3
        var item = TestData.Item(ClothingType.Bottom, season: "Summer");
        var result = _sut.Evaluate(item, Context(temp: 10, season: "Winter"));
        Assert.Equal(-0.3, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_Vetoes_WinterItem_WhenHot()
    {
        var item = TestData.Item(season: "Winter");
        var result = _sut.Evaluate(item, Context(temp: 25, season: "Summer"));
        Assert.Equal(-1.0, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_WrongSeason_GetsGeneralPenalty()
    {
        // temp 18 (not <15, not >22), season Winter, suggestion Spring -> -0.2; base 0.5 -> 0.3
        var item = TestData.Item(season: "Winter");
        var result = _sut.Evaluate(item, Context(temp: 18, season: "Spring"));
        Assert.Equal(0.3, result!.Value, 3);
    }

    [Fact]
    public void Evaluate_RainGear_GetsBoost()
    {
        // empty season -> stays at base 0.5; rain outerwear named "rain" -> +0.4 = 0.9
        var item = TestData.Item(ClothingType.Outerwear, season: "", name: "rain jacket");
        var result = _sut.Evaluate(item, Context(temp: 15, condition: "Rain", season: "Autumn"));
        Assert.Equal(0.9, result!.Value, 3);
    }
}
