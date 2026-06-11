using System.Text.Json;
using System.Text.RegularExpressions;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Infrastructure.ExternalServices;

public sealed class OccasionClassifier : IOccasionClassifier
{
    private readonly List<(string Keyword, string Style)> _rules;

    public OccasionClassifier(string mapFilePath)
    {
        _rules = Load(mapFilePath);
    }

    public string? ClassifyStyle(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt) || _rules.Count == 0) return null;

        var text = prompt.ToLowerInvariant();
        foreach (var (keyword, style) in _rules)
        {
            if (Regex.IsMatch(text, $@"\b{Regex.Escape(keyword)}\b", RegexOptions.CultureInvariant))
                return style;
        }
        return null;
    }

    private static List<(string, string)> Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new();

            var json = File.ReadAllText(path);
            var map = JsonSerializer.Deserialize<OccasionMap>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var rules = new List<(string, string)>();
            var occasions = map?.Occasions;
            if (occasions != null)
            {
                foreach (var occ in occasions)
                {
                    if (string.IsNullOrWhiteSpace(occ.Style) || occ.Keywords == null) continue;
                    foreach (var kw in occ.Keywords)
                    {
                        if (!string.IsNullOrWhiteSpace(kw))
                            rules.Add((kw.Trim().ToLowerInvariant(), occ.Style));
                    }
                }
            }

            // Longest keyword first -> most specific match wins (e.g. "dinner date" before "date").
            return rules.OrderByDescending(r => r.Item1.Length).ToList();
        }
        catch
        {
            return new();
        }
    }

    private class OccasionMap
    {
        public List<OccasionEntry>? Occasions { get; set; }
    }

    private class OccasionEntry
    {
        public string? Style { get; set; }
        public List<string>? Keywords { get; set; }
    }
}
