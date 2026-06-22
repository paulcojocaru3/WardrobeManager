using System;
using System.Collections.Generic;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Scoring;

public sealed class OutfitGenerationContext
{
    public WeatherData? Weather { get; set; }
    public string? TargetStyle { get; set; }
    public IReadOnlyList<string> DesiredColors { get; set; } = new List<string>();
    public IReadOnlyList<string> AvoidColors { get; set; } = new List<string>();

    // soft preference (favorite colors) — rewarded, never penalized
    public IReadOnlyList<string> PreferredColors { get; set; } = new List<string>();

    // persistent user-level colors to avoid — penalized softly, not vetoed
    public IReadOnlyList<string> SoftAvoidColors { get; set; } = new List<string>();

    public string? Occasion { get; set; }
    public int? Formality { get; set; }

    // the chosen outfit's overall formality rank (0..4), set before ranking per-slot swap alternatives so
    public int? OutfitFormalityRank { get; set; }

    // from the seed item; null = no gender constraint
    public string? TargetGender { get; set; }
    public string? TemperatureHint { get; set; }

    // "auto" (null) | "always" | "never"
    public string? OuterwearMode { get; set; }

    // in "auto" mode, drop outerwear above this temperature (°C)
    public int OuterwearTempThreshold { get; set; } = 23;

    // when true, warmer weather progressively favors lighter/shorter garments (short sleeves, shorts)
    public bool PreferLightOnHotDays { get; set; } = true;

    public IReadOnlyList<ClothingType> RequestedTypes { get; set; } = new List<ClothingType>();

    // per-type slot constraint (required sub-type + desired/avoided colors); restricts that slot's candidates
    public IReadOnlyDictionary<ClothingType, GarmentSpec> GarmentConstraints { get; set; } = new Dictionary<ClothingType, GarmentSpec>();

    // last-worn date per item id; drives the variety boost
    public IReadOnlyDictionary<Guid, DateTime> WearRecency { get; set; } = new Dictionary<Guid, DateTime>();

    public IReadOnlyDictionary<Guid, int> WearCounts { get; set; } = new Dictionary<Guid, int>();
    public double MedianWearCount { get; set; }
    public double VarietyDaysFactor { get; set; } = 1.0;

    public string? OccasionBucket { get; set; }

    public IReadOnlySet<Guid> ExcludedItemIds { get; set; } = new HashSet<Guid>();

    // items surfaced in recent generations (last few days) — softly penalized so non-seed slots rotate
    public IReadOnlySet<Guid> RecentlyRecommendedItemIds { get; set; } = new HashSet<Guid>();

    // "rediscover" mode: strongly prefer rarely/never-worn items.
    public bool PreferUnusedItems { get; set; }

    public List<ClothingItem> SelectedItems { get; set; } = new();

    // behaviour-learned signals (empty on a cold start)

    // canonical unordered pair (min id, max id) -> compatibility in [-1,1]
    public IReadOnlyDictionary<(Guid, Guid), double> PairCompatibility { get; set; } = new Dictionary<(Guid, Guid), double>();

    // normalized color-family / style-tag -> learned taste score in [0,1]
    public IReadOnlyDictionary<string, double> LearnedColorScores { get; set; } = new Dictionary<string, double>();
    public IReadOnlyDictionary<string, double> LearnedStyleScores { get; set; } = new Dictionary<string, double>();
}
