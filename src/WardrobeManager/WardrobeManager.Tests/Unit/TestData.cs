using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit;

// small builders so tests state only the fields they care about.
internal static class TestData
{
    public static ClothingItem Item(
        ClothingType type = ClothingType.Top,
        string? color = null,
        string? usage = null,
        string? season = null,
        string? subType = null,
        string name = "item",
        bool isFavorite = false,
        Guid? id = null,
        string? gender = null,
        Guid? userId = null)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Type = type,
            Color = color,
            Usage = usage,
            Season = season,
            SubType = subType,
            Name = name,
            IsFavorite = isFavorite,
            Gender = gender,
        };
}
