using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Generation;

public record GarmentSpec
{
    public ClothingType Type { get; init; }
    public string? SubType { get; init; }
    public IReadOnlyList<string> DesiredColors { get; init; } = new List<string>();
    public IReadOnlyList<string> AvoidColors { get; init; } = new List<string>();
}
