using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Explaining;

namespace WardrobeManager.Infrastructure.ExternalServices;

// explain selected outfits with deterministic styling templates.
public sealed class TemplateStylingNotesService : IStylingNotesService
{
    private const int MaxNotes = 3;

    private static readonly HashSet<string> NeutralColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "black", "white", "gray", "grey", "charcoal", "navy", "beige", "brown", "cream", "khaki", "ivory",
        "off white", "off-white", "tan", "camel", "stone", "ecru", "denim"
    };

    private static readonly Dictionary<string, string> ColorFamilies = new(StringComparer.OrdinalIgnoreCase)
    {
        ["black"] = "black",
        ["charcoal"] = "gray",
        ["gray"] = "gray",
        ["grey"] = "gray",
        ["white"] = "white",
        ["ivory"] = "white",
        ["cream"] = "cream",
        ["off white"] = "white",
        ["off-white"] = "white",
        ["beige"] = "beige",
        ["tan"] = "beige",
        ["camel"] = "brown",
        ["brown"] = "brown",
        ["khaki"] = "khaki",
        ["stone"] = "beige",
        ["navy"] = "blue",
        ["blue"] = "blue",
        ["teal"] = "teal",
        ["turquoise"] = "teal",
        ["green"] = "green",
        ["olive"] = "olive",
        ["sage"] = "green",
        ["yellow"] = "yellow",
        ["mustard"] = "yellow",
        ["orange"] = "orange",
        ["rust"] = "orange",
        ["red"] = "red",
        ["burgundy"] = "red",
        ["maroon"] = "red",
        ["pink"] = "pink",
        ["purple"] = "purple",
        ["violet"] = "purple",
    };

    public Task<IReadOnlyList<string>> GenerateAsync(OutfitExplanation explanation, CancellationToken ct = default)
    {
        if (explanation.Pieces.Count == 0) return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        var notes = new List<string>();
        AddIfPresent(notes, BuildColorNote(explanation));
        AddIfPresent(notes, BuildSilhouetteNote(explanation));
        AddIfPresent(notes, BuildWeatherNote(explanation.Weather, concise: true));
        AddIfPresent(notes, BuildTradeoffNote(explanation));

        return Task.FromResult<IReadOnlyList<string>>(notes.Distinct().Take(MaxNotes).ToList());
    }

    public Task<OutfitInsight> GenerateInsightAsync(OutfitExplanation explanation, CancellationToken ct = default)
    {
        if (explanation.Pieces.Count == 0) return Task.FromResult(new OutfitInsight());

        var insight = new OutfitInsight
        {
            Headline = BuildHeadline(explanation),
            Items = explanation.Pieces.Select(p => new OutfitItemNote(p.ItemId, p.Slot, BuildItemNote(p))).ToList(),
            WeatherAdvice = WithoutWeatherLabel(BuildWeatherNote(explanation.Weather, concise: false)),
            Tips = Array.Empty<string>(),
            Notes = new[]
            {
                BuildColorNote(explanation),
                BuildSilhouetteNote(explanation),
                BuildTradeoffNote(explanation)
            }.Where(n => !string.IsNullOrWhiteSpace(n)).Cast<string>().Take(MaxNotes).ToList()
        };

        return Task.FromResult(insight);
    }

    private static string BuildHeadline(OutfitExplanation e)
    {
        var palette = AnalyzePalette(e.Pieces);
        var style = string.IsNullOrWhiteSpace(e.Style) ? "balanced" : e.Style.Trim().ToLowerInvariant();

        return palette.Approach switch
        {
            PaletteApproach.Monochrome => $"A {style} monochrome look with clean depth.",
            PaletteApproach.NeutralBaseAccent => $"A {style} palette with a calm base and one clear accent.",
            PaletteApproach.Analogous => $"A {style} look built on neighboring tones.",
            _ => $"A {style} outfit with a controlled palette."
        };
    }

    private static string? BuildColorNote(OutfitExplanation e)
    {
        var palette = AnalyzePalette(e.Pieces);
        if (palette.Colors.Count == 0) return null;

        return palette.Approach switch
        {
            PaletteApproach.Monochrome =>
                $"Color coordination: monochrome, using {Join(palette.Colors)} in related shades so the outfit feels cohesive.",
            PaletteApproach.NeutralBaseAccent =>
                $"Color coordination: neutral base plus accent, with {Join(palette.Neutrals)} grounding {palette.Accent}.",
            PaletteApproach.Analogous =>
                $"Color coordination: analogous tones, pairing {Join(palette.Colors)} because neighboring colors harmonize naturally.",
            _ =>
                $"Color coordination: the palette stays controlled with {Join(palette.Colors.Take(3).ToList())} and avoids too many competing colors."
        };
    }

    private static string? BuildSilhouetteNote(OutfitExplanation e)
    {
        var top = Find(e, "top");
        var bottom = Find(e, "bottoms");
        var outerwear = Find(e, "outerwear");

        if (top == null || bottom == null)
        {
            return outerwear == null
                ? null
                : $"Proportion: the {Name(outerwear)} gives the outfit a clear outer line without adding another competing focal point.";
        }

        var topRelaxed = ReadsRelaxed(top);
        var bottomRelaxed = ReadsRelaxed(bottom);

        if (topRelaxed && !bottomRelaxed)
            return $"Proportion: the relaxed {Name(top)} works against the cleaner {Name(bottom)} so the silhouette does not feel bulky.";
        if (!topRelaxed && bottomRelaxed)
            return $"Proportion: the cleaner {Name(top)} balances the easier shape of the {Name(bottom)}.";
        if (outerwear != null)
            return $"Silhouette: keep the {Name(outerwear)} open so the {Name(top)} and {Name(bottom)} still define the line.";

        return $"Silhouette: the {Name(top)} and {Name(bottom)} keep the outfit balanced from top to bottom.";
    }

    private static string? BuildWeatherNote(WeatherData? weather, bool concise)
    {
        if (weather == null) return null;

        var feels = weather.FeelsLike ?? weather.Temperature;
        if (weather.RainChance is >= 50)
        {
            var rain = RainIntensity(weather).ToLowerInvariant();
            return concise
                ? $"Weather: {weather.RainChance}% chance of {rain}, so keep the shoes and outer layer practical."
                : $"Weather: there is a {weather.RainChance}% chance of {rain}; waterproof shoes or a reliable outer layer make this safer outside.";
        }

        if (feels <= 8)
        {
            return concise
                ? $"Weather: it feels like {(int)Math.Round(feels)}C, so the outfit benefits from a warm layer."
                : $"Weather: it feels like {(int)Math.Round(feels)}C; prioritize layering and keep the outerwear easy to remove indoors.";
        }

        if (feels >= 28)
        {
            return concise
                ? $"Weather: it feels like {(int)Math.Round(feels)}C, so lighter fabrics and an open silhouette matter."
                : $"Weather: it feels like {(int)Math.Round(feels)}C; keep the fit breathable and avoid heavy layering.";
        }

        if (weather.RainChance is >= 25)
        {
            return concise
                ? $"Weather: a {weather.RainChance}% rain chance makes a practical outer layer useful."
                : $"Weather: a {weather.RainChance}% rain chance is not severe, but a practical outer layer keeps the look ready.";
        }

        return concise
            ? "Weather: the conditions are mild enough for the outfit as built."
            : "Weather: conditions are mild, so the outfit can stay focused on proportion and color rather than protection.";
    }

    private static string? BuildTradeoffNote(OutfitExplanation e)
    {
        if (e.Tradeoffs.Count == 0) return null;
        return "Constraint handling: the outfit keeps the strongest available pieces while preserving the overall styling direction.";
    }

    private static string BuildItemNote(OutfitPieceFact piece)
    {
        var garment = DisplayGarment(piece);
        return piece.Slot switch
        {
            "top" => $"{garment} frames the upper half and keeps the palette coherent.",
            "bottoms" => $"{garment} anchor the silhouette and add structure.",
            "shoes" => $"{garment} finish the outfit and keep it practical.",
            "outerwear" => $"{garment} adds the outer line and weather-ready layer.",
            "accessory" => $"{garment} adds a small finishing detail without taking over.",
            _ => $"{garment} supports the outfit without competing."
        };
    }

    private static string DisplayGarment(OutfitPieceFact piece)
    {
        var text = !string.IsNullOrWhiteSpace(piece.Description) ? piece.Description! : Name(piece);
        text = text.Trim().ToLowerInvariant()
            .Replace("tshirts", "t-shirt")
            .Replace("tshirt", "t-shirt")
            .Replace("tee shirts", "t-shirts")
            .Replace("casual shoes", "casual shoes");

        return char.ToUpperInvariant(text[0]) + text[1..];
    }

    private static string? WithoutWeatherLabel(string? note)
    {
        const string prefix = "Weather: ";
        if (string.IsNullOrWhiteSpace(note)) return note;
        return note.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? note[prefix.Length..] : note;
    }

    private static OutfitPieceFact? Find(OutfitExplanation e, string slot) =>
        e.Pieces.FirstOrDefault(p => string.Equals(p.Slot, slot, StringComparison.OrdinalIgnoreCase));

    private static bool ReadsRelaxed(OutfitPieceFact piece)
    {
        var text = $"{piece.Name} {piece.SubType} {piece.Description} {piece.Style}".ToLowerInvariant();
        return text.Contains("oversized") ||
               text.Contains("relaxed") ||
               text.Contains("loose") ||
               text.Contains("wide") ||
               text.Contains("baggy") ||
               text.Contains("sweat") ||
               text.Contains("hoodie") ||
               text.Contains("jogger");
    }

    private static PaletteAnalysis AnalyzePalette(IReadOnlyList<OutfitPieceFact> pieces)
    {
        var colors = pieces
            .Select(p => NormalizeColor(p.Color))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var families = colors
            .Select(c => ColorFamilies.TryGetValue(c, out var family) ? family : c)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var neutrals = colors.Where(IsNeutral).ToList();
        var accents = colors.Where(c => !IsNeutral(c)).ToList();

        var approach = PaletteApproach.Controlled;
        if (families.Count == 1 && colors.Count > 1)
            approach = PaletteApproach.Monochrome;
        else if (neutrals.Count > 0 && accents.Count == 1)
            approach = PaletteApproach.NeutralBaseAccent;
        else if (IsAnalogous(families))
            approach = PaletteApproach.Analogous;

        return new PaletteAnalysis(approach, colors, neutrals, accents.FirstOrDefault());
    }

    private static bool IsAnalogous(IReadOnlyList<string> families)
    {
        if (families.Count < 2 || families.Count > 3) return false;

        var set = families.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return ContainsAll(set, "blue", "teal") ||
               ContainsAll(set, "teal", "green") ||
               ContainsAll(set, "green", "olive") ||
               ContainsAll(set, "olive", "khaki") ||
               ContainsAll(set, "orange", "red") ||
               ContainsAll(set, "orange", "brown") ||
               ContainsAll(set, "red", "purple") ||
               ContainsAll(set, "yellow", "orange");
    }

    private static bool ContainsAll(HashSet<string> set, params string[] values) =>
        values.All(set.Contains);

    private static bool IsNeutral(string color) => NeutralColors.Contains(color);

    private static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;
        var normalized = color.Trim().ToLowerInvariant().Replace('_', ' ');
        return normalized;
    }

    private static string RainIntensity(WeatherData weather)
    {
        var detail = weather.ConditionDetail;
        if (!string.IsNullOrWhiteSpace(detail))
        {
            if (detail.Contains("heavy", StringComparison.OrdinalIgnoreCase) ||
                detail.Contains("thunder", StringComparison.OrdinalIgnoreCase))
                return "heavy rain";
            if (detail.Contains("shower", StringComparison.OrdinalIgnoreCase) ||
                detail.Contains("light", StringComparison.OrdinalIgnoreCase) ||
                detail.Contains("drizzle", StringComparison.OrdinalIgnoreCase))
                return "light showers";
        }

        if (weather.PrecipitationMm is > 4) return "heavy rain";
        if (weather.PrecipitationMm is > 0) return "light showers";
        return "rain";
    }

    private static string Name(OutfitPieceFact piece)
    {
        if (!string.IsNullOrWhiteSpace(piece.Color))
            return $"{piece.Color.Trim().ToLowerInvariant()} {piece.Slot}";
        return string.IsNullOrWhiteSpace(piece.Name) ? piece.Slot : piece.Name.Trim().ToLowerInvariant();
    }

    private static string Join(IReadOnlyList<string> values)
    {
        if (values.Count == 0) return "";
        if (values.Count == 1) return values[0];
        return string.Join(", ", values.Take(values.Count - 1)) + " and " + values[^1];
    }

    private static void AddIfPresent(List<string> notes, string? note)
    {
        if (!string.IsNullOrWhiteSpace(note)) notes.Add(note);
    }

    private sealed record PaletteAnalysis(
        PaletteApproach Approach,
        IReadOnlyList<string> Colors,
        IReadOnlyList<string> Neutrals,
        string? Accent);

    private enum PaletteApproach
    {
        Controlled,
        Monochrome,
        NeutralBaseAccent,
        Analogous
    }
}
