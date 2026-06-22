using WardrobeManager.Application.PlannedOutfits;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Planning;

[Trait("Category", "Unit")]
public sealed class EventReusePolicyTests
{
    private static ClothingItem Item(ClothingType type) => new() { Id = Guid.NewGuid(), Type = type };

    [Fact]
    public void TopAndBottom_AreExcludedBeforeCooldown_AndReusableExactlyOnDayN()
    {
        var top = Item(ClothingType.Top);
        var bottom = Item(ClothingType.Bottom);
        var start = new DateTime(2026, 6, 1);
        var usage = new Dictionary<DateTime, IReadOnlyCollection<ClothingItem>>
        {
            [start] = new[] { top, bottom }
        };

        var dayTwo = EventReusePolicy.ComputeExcludedItemIds(usage, start.AddDays(2), 3);
        var dayThree = EventReusePolicy.ComputeExcludedItemIds(usage, start.AddDays(3), 3);

        Assert.Contains(top.Id, dayTwo);
        Assert.Contains(bottom.Id, dayTwo);
        Assert.DoesNotContain(top.Id, dayThree);
        Assert.DoesNotContain(bottom.Id, dayThree);
    }

    [Fact]
    public void ShoesOuterwearAndAccessories_AreNeverExcludedByCooldown()
    {
        var reusable = new[]
        {
            Item(ClothingType.Shoes),
            Item(ClothingType.Outerwear),
            Item(ClothingType.Accessory)
        };
        var date = new DateTime(2026, 6, 1);
        var usage = new Dictionary<DateTime, IReadOnlyCollection<ClothingItem>> { [date] = reusable };

        var excluded = EventReusePolicy.ComputeExcludedItemIds(usage, date.AddDays(1), 14);

        Assert.All(reusable, item => Assert.DoesNotContain(item.Id, excluded));
    }

    [Fact]
    public void DisabledReuse_ExcludesTopsAndBottomsAcrossTheWholeEvent()
    {
        var top = Item(ClothingType.Top);
        var date = new DateTime(2026, 6, 1);
        var usage = new Dictionary<DateTime, IReadOnlyCollection<ClothingItem>> { [date] = new[] { top } };

        var excluded = EventReusePolicy.ComputeExcludedItemIds(usage, date.AddDays(20), null);

        Assert.Contains(top.Id, excluded);
    }
}
