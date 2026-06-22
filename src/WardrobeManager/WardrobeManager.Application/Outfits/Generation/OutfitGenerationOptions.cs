using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Generation;

public record OutfitGenerationOptions
{
    // minimum per-piece score below which the outfit is flagged not-fully-valid
    public double Threshold { get; init; } = 0.5;

    public WeatherData? Weather { get; init; }
    public string? Style { get; init; }
    public IReadOnlyList<string> DesiredColors { get; init; } = new List<string>();
    public IReadOnlyList<string> AvoidColors { get; init; } = new List<string>();
    public string? Occasion { get; init; }

    // 1 (very casual) – 5 (very formal), or null
    public int? Formality { get; init; }
    public string? TemperatureHint { get; init; }
    public IReadOnlyList<ClothingType> RequestedTypes { get; init; } = new List<ClothingType>();

    // when true, strongly favor rarely/never-worn items across every slot ("rediscover" mode)
    public bool PreferUnusedItems { get; init; }

    // hard exclusions used by event packing/cooldown rules.
    public IReadOnlySet<Guid> ExcludedItemIds { get; init; } = new HashSet<Guid>();

    // per-type slot constraint (required sub-type + desired/avoided colors); restricts that slot's candidates
    public IReadOnlyDictionary<ClothingType, GarmentSpec> GarmentConstraints { get; init; } = new Dictionary<ClothingType, GarmentSpec>();
}
