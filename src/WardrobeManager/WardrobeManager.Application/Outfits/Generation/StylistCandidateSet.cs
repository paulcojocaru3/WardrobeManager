using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Generation;

// map fashionclip candidates to gemma3 numbers and back.
public sealed class StylistCandidateSet
{
    private readonly Dictionary<int, ClothingItem> _byNumber;

    public IReadOnlyList<StylistItem> Lines { get; }

    private StylistCandidateSet(IReadOnlyList<StylistItem> lines, Dictionary<int, ClothingItem> byNumber)
    {
        Lines = lines;
        _byNumber = byNumber;
    }

    public static StylistCandidateSet Build(IReadOnlyList<ClothingItem> items, bool shuffle = false)
    {
        var list = items.ToList();
        if (shuffle)
        {
            Random.Shared.Shuffle(CollectionsMarshal.AsSpan(list));
        }

        var numbered = list.Select((item, i) => (Number: i + 1, Item: item)).ToList();
        var byNumber = numbered.ToDictionary(x => x.Number, x => x.Item);

        // mark same-slot near-duplicates for varied generated looks.
        var neighbors = ComputeVisualNeighbors(numbered);

        var lines = new List<StylistItem>(numbered.Count);
        foreach (var (number, item) in numbered)
        {
            var line = DescribeItem(item);
            if (neighbors.TryGetValue(number, out var pair) && pair.Count > 0)
            {
                line += " | similar alternatives: " + string.Join(", ", pair.Select(n => $"[{n}]"));
            }
            lines.Add(new StylistItem(number, item.Type.ToString().ToUpperInvariant(), line));
        }

        return new StylistCandidateSet(lines, byNumber);
    }

    // find the gemma3 candidate number for an item.
    public int? NumberOf(Guid itemId)
    {
        foreach (var kv in _byNumber)
        {
            if (kv.Value.Id == itemId) return kv.Key;
        }
        return null;
    }

    // resolve gemma3 numbers and drop invented values.
    public List<ClothingItem> Resolve(IEnumerable<int> numbers)
    {
        var result = new List<ClothingItem>();
        var seen = new HashSet<Guid>();
        foreach (var n in numbers)
        {
            if (_byNumber.TryGetValue(n, out var item) && seen.Add(item.Id))
            {
                result.Add(item);
            }
        }
        return result;
    }

    private static string DescribeItem(ClothingItem item)
    {
        var parts = new List<string>();
        parts.Add($"slot={item.Type.ToString().ToUpperInvariant()}");
        // User-facing names can become stale after ML/Gemma attributes are corrected (for example,
        // "black jeans" with Color="khaki"). Keep the stylist prompt grounded only in canonical fields.
        parts.Add($"subtype={(string.IsNullOrWhiteSpace(item.SubType) ? item.Type.ToString().ToLowerInvariant() : item.SubType)}");
        parts.Add($"color={(string.IsNullOrWhiteSpace(item.Color) ? "unknown" : item.Color)}");
        if (!string.IsNullOrWhiteSpace(item.SecondaryColor)) parts.Add($"secondary_color={item.SecondaryColor}");
        parts.Add($"pattern={(string.IsNullOrWhiteSpace(item.Pattern) ? "solid_or_unknown" : item.Pattern)}");
        parts.Add($"material={(string.IsNullOrWhiteSpace(item.Material) ? "unknown" : item.Material)}");
        parts.Add($"formality={FormalityRank(item)} {FormalityLabel(item)}");
        if (!string.IsNullOrWhiteSpace(item.Usage)) parts.Add($"usage={item.Usage}");
        if (!string.IsNullOrWhiteSpace(item.Season)) parts.Add($"season={item.Season}");
        if (!string.IsNullOrWhiteSpace(item.Gender)) parts.Add($"fit={item.Gender}");

        return string.Join(" | ", parts);
    }

    private static int FormalityRank(ClothingItem item) => FormalityScale.RankOf(item) + 1;

    private static string FormalityLabel(ClothingItem item)
    {
        var rank = FormalityScale.RankOf(item);
        return rank switch
        {
            0 => "very casual",
            1 => "casual",
            2 => "smart casual",
            3 => "business casual",
            _ => "formal"
        };
    }

    private static Dictionary<int, List<int>> ComputeVisualNeighbors(
        List<(int Number, ClothingItem Item)> numbered)
    {
        var result = new Dictionary<int, List<int>>();
        foreach (var (number, item) in numbered)
        {
            if (item.Embedding == null) continue;

            var ranked = numbered
                .Where(o => o.Number != number && o.Item.Embedding != null && o.Item.Type == item.Type)
                .Select(o => (o.Number, Sim: VectorSimilarity.Cosine(item.Embedding!, o.Item.Embedding!)))
                .OrderByDescending(x => x.Sim)
                .Take(2)
                .Where(x => x.Sim > 0.0)
                .Select(x => x.Number)
                .ToList();

            if (ranked.Count > 0) result[number] = ranked;
        }
        return result;
    }

}
