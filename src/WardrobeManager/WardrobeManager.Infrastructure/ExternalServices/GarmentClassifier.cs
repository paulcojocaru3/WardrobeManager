using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing;
using WardrobeManager.Application.Outfits.Prompting;

namespace WardrobeManager.Infrastructure.ExternalServices;

public sealed class GarmentClassifier : IGarmentClassifier
{
    // (keyword, subType) ordered by keyword length desc so the most specific match wins.
    private readonly List<(string Keyword, string SubType)> _rules;

    public GarmentClassifier(string mapFilePath)
    {
        _rules = Load(mapFilePath);
    }

    public IReadOnlyList<RequestedGarment> Detect(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt) || _rules.Count == 0)
            return Array.Empty<RequestedGarment>();

        var text = Fold(prompt);

        // subType -> earliest position it was matched at, so we can order by appearance.
        var firstPos = new Dictionary<string, int>();
        foreach (var (keyword, subType) in _rules)
        {
            var m = Regex.Match(text, $@"\b{Regex.Escape(keyword)}\b", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
            if (!m.Success) continue;
            if (!firstPos.TryGetValue(subType, out var existing) || m.Index < existing)
                firstPos[subType] = m.Index;
        }

        return firstPos
            .OrderBy(kv => kv.Value)
            .Select(kv => new RequestedGarment(kv.Key, ArticleTypeMap.ToClothingType(kv.Key)))
            .ToList();
    }

    // Lowercase + strip diacritics (ă/â/î/ș/ț -> a/a/i/s/t).
    private static string Fold(string s)
    {
        var normalized = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static List<(string, string)> Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new();

            var json = File.ReadAllText(path);
            var map = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var rules = new List<(string, string)>();
            if (map != null)
            {
                foreach (var (subType, keywords) in map)
                {
                    if (string.IsNullOrWhiteSpace(subType) || keywords == null) continue;
                    foreach (var kw in keywords)
                    {
                        if (!string.IsNullOrWhiteSpace(kw))
                            rules.Add((Fold(kw.Trim()), subType.Trim().ToLowerInvariant()));
                    }
                }
            }

            return rules.OrderByDescending(r => r.Item1.Length).ToList();
        }
        catch
        {
            return new();
        }
    }
}
