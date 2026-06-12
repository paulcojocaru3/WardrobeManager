using WardrobeManager.Application.Clothing.Queries;

namespace WardrobeManager.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class StatsWindowResolverTests
{
    [Fact]
    public void Resolve_NoRangeNoDates_IsEmptyButValid()
    {
        var r = StatsWindowResolver.Resolve(null, null, null);
        Assert.True(r.IsValid);
        Assert.Null(r.StartUtc);
        Assert.Null(r.EndUtc);
    }

    [Fact]
    public void Resolve_UnknownRange_IsInvalid()
    {
        var r = StatsWindowResolver.Resolve("yesterday", null, null);
        Assert.False(r.IsValid);
        Assert.Contains("Invalid range", r.Error);
    }

    [Theory]
    [InlineData("7d", 7)]
    [InlineData("30d", 30)]
    [InlineData("90d", 90)]
    public void Resolve_RelativeRange_ProducesWindowOfExpectedLength(string range, int days)
    {
        var r = StatsWindowResolver.Resolve(range, null, null);

        Assert.True(r.IsValid);
        Assert.NotNull(r.StartUtc);
        Assert.NotNull(r.EndUtc);
        Assert.Equal(days, (r.EndUtc!.Value - r.StartUtc!.Value).TotalDays, 0);
    }

    [Fact]
    public void Resolve_OneYearRange_IsValid()
    {
        var r = StatsWindowResolver.Resolve("1y", null, null);
        Assert.True(r.IsValid);
        Assert.True(r.StartUtc < r.EndUtc);
    }

    [Fact]
    public void Resolve_CustomKeyword_WithoutDates_IsInvalid()
    {
        var r = StatsWindowResolver.Resolve("custom", null, null);
        Assert.False(r.IsValid);
        Assert.Contains("requires both", r.Error);
    }

    [Fact]
    public void Resolve_PartialCustomDates_IsInvalid()
    {
        var r = StatsWindowResolver.Resolve(null, new DateTime(2026, 1, 1), null);
        Assert.False(r.IsValid);
        Assert.Contains("Both customStart and customEnd", r.Error);
    }

    [Fact]
    public void Resolve_CustomDates_WithConflictingRange_IsInvalid()
    {
        var r = StatsWindowResolver.Resolve("30d", new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        Assert.False(r.IsValid);
        Assert.Contains("range must be omitted", r.Error);
    }

    [Fact]
    public void Resolve_CustomEndBeforeStart_IsInvalid()
    {
        var r = StatsWindowResolver.Resolve(null, new DateTime(2026, 2, 1), new DateTime(2026, 1, 1));
        Assert.False(r.IsValid);
        Assert.Contains("customEnd must be greater", r.Error);
    }

    [Fact]
    public void Resolve_ValidCustomDates_SpanWholeEndDay()
    {
        var r = StatsWindowResolver.Resolve("custom", new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        Assert.True(r.IsValid);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), r.StartUtc);
        Assert.Equal(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1), r.EndUtc);
    }
}
