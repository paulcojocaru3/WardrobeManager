using System.Text.Json;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Feasibility;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Infrastructure.ExternalServices;

// config-driven thermal rules loaded from Data/thermal-rules.json at startup. Falls back to the
public sealed class ThermalRules : IThermalRules
{
    private readonly IReadOnlyList<string> _warmOnlySubTypes;
    private readonly IReadOnlyList<string> _warmOnlyNameHints;
    private readonly IReadOnlyList<string> _winterSeasons;

    public double FreezingC { get; }
    public double ColdC { get; }
    public double HotC { get; }

    public ThermalRules(string configFilePath)
    {
        var cfg = Load(configFilePath) ?? Config.Defaults;
        FreezingC = cfg.FreezingC ?? 10;
        ColdC = cfg.ColdC ?? 15;
        HotC = cfg.HotC ?? 22;
        _warmOnlySubTypes = Normalize(cfg.WarmOnlySubTypes) ?? new[] { "shorts", "sandals", "flip flops" };
        _warmOnlyNameHints = Normalize(cfg.WarmOnlyNameHints) ?? new[] { "shorts", "sandals" };
        _winterSeasons = cfg.WinterSeasons is { Count: > 0 } ws ? ws : new List<string> { "Winter" };
    }

    public bool IsWarmOnly(ClothingItem item)
    {
        var subType = item.SubType ?? "";
        if (_warmOnlySubTypes.Contains(subType, StringComparer.OrdinalIgnoreCase)) return true;

        var name = item.Name ?? "";
        return _warmOnlyNameHints.Any(h => name.Contains(h, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsWeatherVetoed(ClothingItem item, WeatherData weather)
    {
        if (weather.Temperature < FreezingC && IsWarmOnly(item)) return true;

        var season = item.Season ?? "";
        if (weather.Temperature > HotC &&
            _winterSeasons.Any(w => season.Contains(w, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    private static IReadOnlyList<string>? Normalize(List<string>? values) =>
        values is { Count: > 0 } ? values.Select(v => v.Trim()).Where(v => v.Length > 0).ToList() : null;

    private static Config? Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Config>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private sealed class Config
    {
        public static readonly Config Defaults = new();

        public double? FreezingC { get; set; }
        public double? ColdC { get; set; }
        public double? HotC { get; set; }
        public List<string>? WarmOnlySubTypes { get; set; }
        public List<string>? WarmOnlyNameHints { get; set; }
        public List<string>? WinterSeasons { get; set; }
    }
}
