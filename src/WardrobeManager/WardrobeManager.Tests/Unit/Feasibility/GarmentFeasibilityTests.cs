using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Feasibility;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;
using WardrobeManager.Tests.Unit.TestSupport;

namespace WardrobeManager.Tests.Unit.Feasibility;

[Trait("Category", "Unit")]
public sealed class GarmentFeasibilityTests
{
    private readonly IGarmentFeasibility _sut = Defaults.Feasibility;

    [Fact]
    public void Check_Feasible_WhenNoConstraints()
        => Assert.True(_sut.Check(TestData.Item(), new OutfitGenerationContext()).IsFeasible);

    [Fact]
    public void Check_WarmOnlyGarmentWhenFreezing_FlagsWeather()
    {
        var shorts = TestData.Item(ClothingType.Bottom, subType: "shorts");
        var ctx = new OutfitGenerationContext { Weather = new WeatherData(2, "Clear", "Winter") };
        Assert.Contains(ConstraintKind.Weather, _sut.Check(shorts, ctx).Violations);
    }

    [Fact]
    public void Check_GenderLock_FlagsGender_ButUnisexIsFeasible()
    {
        var ctx = new OutfitGenerationContext { TargetGender = "Men" };
        Assert.Contains(ConstraintKind.Gender, _sut.Check(TestData.Item(gender: "Women"), ctx).Violations);
        Assert.True(_sut.Check(TestData.Item(gender: "Unisex"), ctx).IsFeasible);
    }

    [Fact]
    public void Check_HardStyleClash_FlagsStyle()
    {
        var ctx = new OutfitGenerationContext { TargetStyle = "Formal" };
        Assert.Contains(ConstraintKind.Style, _sut.Check(TestData.Item(usage: "Sports"), ctx).Violations);
    }

    [Fact]
    public void FilterWithRelaxation_PrefersFullyFeasible_OverRelaxed()
    {
        var blue = TestData.Item(ClothingType.Bottom, color: "blue");
        var black = TestData.Item(ClothingType.Bottom, color: "black");
        var ctx = Desired("blue");

        var pool = _sut.FilterWithRelaxation(new List<(ClothingItem, double)> { (black, 0.9), (blue, 0.8) }, ctx);

        Assert.Single(pool);
        Assert.Equal(blue.Id, pool[0].Item.Id); // only the feasible blue survives at relaxation level 0
        Assert.Empty(pool[0].Relaxed);
    }

    [Fact]
    public void FilterWithRelaxation_RelaxesLowestPriorityFirst_WhenNoFeasibleItem()
    {
        var black = TestData.Item(ClothingType.Bottom, color: "black");
        var ctx = Desired("blue");

        var pool = _sut.FilterWithRelaxation(new List<(ClothingItem, double)> { (black, 0.9) }, ctx);

        Assert.Single(pool);
        Assert.Contains(ConstraintKind.DesiredColor, pool[0].Relaxed); // desired color relaxed to stay non-empty
    }

    [Fact]
    public void FilterWithRelaxation_Empty_WhenNoCandidates()
        => Assert.Empty(_sut.FilterWithRelaxation(new List<(ClothingItem, double)>(), new OutfitGenerationContext()));

    private static OutfitGenerationContext Desired(string color) => new()
    {
        GarmentConstraints = new Dictionary<ClothingType, GarmentSpec>
        {
            [ClothingType.Bottom] = new() { Type = ClothingType.Bottom, DesiredColors = new[] { color } },
        },
    };
}
