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

    public string? Occasion { get; set; }
    public int? Formality { get; set; }

    // from the seed item; null = no gender constraint
    public string? TargetGender { get; set; }
    public string? TemperatureHint { get; set; }

    // "auto" (null) | "always" | "never"
    public string? OuterwearMode { get; set; }

    // in "auto" mode, drop outerwear above this temperature (°C)
    public int OuterwearTempThreshold { get; set; } = 23;

    public IReadOnlyList<ClothingType> RequestedTypes { get; set; } = new List<ClothingType>();

    // per-type slot constraint (required sub-type + desired/avoided colors); restricts that slot's candidates
    public IReadOnlyDictionary<ClothingType, GarmentSpec> GarmentConstraints { get; set; } = new Dictionary<ClothingType, GarmentSpec>();

    // last-worn date per item id; drives the variety boost
    public IReadOnlyDictionary<Guid, DateTime> WearRecency { get; set; } = new Dictionary<Guid, DateTime>();

    public List<ClothingItem> SelectedItems { get; set; } = new();

    // learned weights; null = use each evaluator's default
    public IReadOnlyDictionary<string, double>? LearnedWeights { get; set; }
    public double? LearnedMlWeight { get; set; }
}
