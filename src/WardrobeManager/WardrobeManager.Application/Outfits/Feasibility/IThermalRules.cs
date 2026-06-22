using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Feasibility;

// centralizes garment ↔ temperature rules so the thresholds and warm-only garment list live in one
public interface IThermalRules
{
    // below this (°C) warm-only garments (shorts/sandals/…) are unwearable.
    double FreezingC { get; }

    // below this (°C) summer pieces are penalized (soft).
    double ColdC { get; }

    // above this (°C) winter pieces are unwearable.
    double HotC { get; }

    // a garment that only makes sense in warm weather (by sub-type or name hint).
    bool IsWarmOnly(ClothingItem item);

    // hard rule: the item cannot be worn in this weather (warm-only when freezing, winter when hot).
    bool IsWeatherVetoed(ClothingItem item, WeatherData weather);
}
