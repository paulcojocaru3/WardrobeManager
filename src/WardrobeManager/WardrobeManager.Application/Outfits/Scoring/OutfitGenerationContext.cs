using System.Collections.Generic;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

public class OutfitGenerationContext
{
    public WeatherData? Weather { get; set; }
    public string? TargetStyle { get; set; }
    public IReadOnlyList<string> DesiredColors { get; set; } = new List<string>();
    public IReadOnlyList<string> AvoidColors { get; set; } = new List<string>();
    public string? Occasion { get; set; }
    public int? Formality { get; set; }
    public List<ClothingItem> SelectedItems { get; set; } = new();
}
