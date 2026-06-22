using System.Text.Json;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Infrastructure.ExternalServices;

// config-driven occasion -> formality (1..5) mapping loaded from Data/occasion-formality.json at startup.
public sealed class OccasionFormalityRules : IOccasionFormalityRules
{
    private readonly IReadOnlyDictionary<string, int> _buckets;

    public OccasionFormalityRules(string configFilePath)
    {
        _buckets = Load(configFilePath) ?? Defaults;
    }

    public int? FormalityFor(string? occasion)
    {
        if (string.IsNullOrWhiteSpace(occasion)) return null;
        var key = occasion.Trim().ToLowerInvariant();
        return _buckets.TryGetValue(key, out var level) ? level : (int?)null;
    }

    private static readonly IReadOnlyDictionary<string, int> Defaults = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["sports"] = 1, ["gym"] = 1, ["athleisure"] = 1, ["loungewear"] = 1,
        ["casual"] = 2, ["travel"] = 2, ["everyday"] = 2, ["general"] = 2, ["outdoor"] = 2,
        ["smart"] = 3, ["smart casual"] = 3, ["work"] = 3, ["office"] = 3, ["business casual"] = 3, ["date"] = 3,
        ["party"] = 4, ["ethnic"] = 4, ["cocktail"] = 4,
        ["formal"] = 5, ["business formal"] = 5, ["wedding"] = 5, ["interview"] = 5,
    };

    private static IReadOnlyDictionary<string, int>? Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            var cfg = JsonSerializer.Deserialize<Config>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (cfg?.Buckets is not { Count: > 0 }) return null;

            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in cfg.Buckets)
            {
                var key = kv.Key.Trim().ToLowerInvariant();
                if (key.Length > 0) map[key] = Math.Clamp(kv.Value, 1, 5);
            }
            return map.Count > 0 ? map : null;
        }
        catch
        {
            return null;
        }
    }

    private sealed class Config
    {
        public Dictionary<string, int>? Buckets { get; set; }
    }
}
