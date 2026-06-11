using System;

namespace WardrobeManager.Application.Outfits.Scoring;

public static class ColorFamily
{
    private static readonly string[] Neutrals =
    {
        "black", "white", "off-white", "cream", "ivory", "gray", "grey", "silver",
        "charcoal", "navy", "beige", "tan", "taupe", "sand", "brown"
    };

    private static readonly (string Token, string Family)[] FamilyTokens =
    {
        ("maroon", "red"), ("burgundy", "red"), ("crimson", "red"), ("scarlet", "red"),
        ("ruby", "red"), ("wine", "red"), ("brick", "red"), ("red", "red"),
        ("tangerine", "orange"), ("terracotta", "orange"), ("apricot", "orange"),
        ("peach", "orange"), ("rust", "orange"), ("orange", "orange"),
        ("mustard", "yellow"), ("golden", "yellow"), ("gold", "yellow"),
        ("lemon", "yellow"), ("amber", "yellow"), ("yellow", "yellow"),
        ("olive", "green"), ("emerald", "green"), ("lime", "green"), ("mint", "green"),
        ("forest", "green"), ("khaki", "green"), ("teal", "green"), ("green", "green"),
        ("azure", "blue"), ("cyan", "blue"), ("turquoise", "blue"), ("denim", "blue"),
        ("indigo", "blue"), ("sky", "blue"), ("blue", "blue"),
        ("violet", "purple"), ("lavender", "purple"), ("plum", "purple"),
        ("eggplant", "purple"), ("mauve", "purple"), ("lilac", "purple"), ("purple", "purple"),
        ("fuchsia", "pink"), ("magenta", "pink"), ("salmon", "pink"),
        ("coral", "pink"), ("rose", "pink"), ("pink", "pink"),
    };

    // shade/synonym -> basic color word, so a "navy" item satisfies a "blue" request and "charcoal"
    // satisfies "black". Sorted longest-token-first so "tangerine" wins over the "tan" substring.
    private static readonly (string Token, string Basic)[] BasicColorTokens =
        new (string Token, string Basic)[]
        {
            ("navy", "blue"), ("azure", "blue"), ("cobalt", "blue"), ("indigo", "blue"),
            ("denim", "blue"), ("cerulean", "blue"), ("sky", "blue"), ("blue", "blue"),
            ("charcoal", "black"), ("onyx", "black"), ("ebony", "black"), ("black", "black"),
            ("off-white", "white"), ("off white", "white"), ("ivory", "white"), ("cream", "white"),
            ("eggshell", "white"), ("snow", "white"), ("white", "white"),
            ("gunmetal", "gray"), ("silver", "gray"), ("slate", "gray"), ("grey", "gray"), ("gray", "gray"),
            ("maroon", "red"), ("burgundy", "red"), ("crimson", "red"), ("scarlet", "red"),
            ("ruby", "red"), ("wine", "red"), ("brick", "red"), ("red", "red"),
            ("olive", "green"), ("emerald", "green"), ("lime", "green"), ("forest", "green"),
            ("sage", "green"), ("mint", "green"), ("green", "green"),
            ("mustard", "yellow"), ("golden", "yellow"), ("gold", "yellow"), ("amber", "yellow"),
            ("lemon", "yellow"), ("yellow", "yellow"),
            ("tangerine", "orange"), ("terracotta", "orange"), ("apricot", "orange"), ("rust", "orange"), ("orange", "orange"),
            ("violet", "purple"), ("lavender", "purple"), ("plum", "purple"), ("mauve", "purple"),
            ("lilac", "purple"), ("purple", "purple"),
            ("fuchsia", "pink"), ("salmon", "pink"), ("blush", "pink"), ("rose", "pink"), ("pink", "pink"),
            ("taupe", "brown"), ("camel", "brown"), ("chocolate", "brown"), ("coffee", "brown"),
            ("beige", "brown"), ("sand", "brown"), ("tan", "brown"), ("brown", "brown"),
        }.OrderByDescending(t => t.Token.Length).ToArray();

    // The basic color word a color string maps to (e.g. "navy" -> "blue"), or null when unknown.
    public static string? BasicColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;
        foreach (var (token, basic) in BasicColorTokens)
            if (color.Contains(token, StringComparison.OrdinalIgnoreCase)) return basic;
        return null;
    }

    // Match used for color constraints. First a direct substring (so "navy blue" matches "blue"),
    // then a shade-aware fallback so a "navy" item satisfies a "blue" request and "charcoal" "black".
    public static bool ColorsMatch(string? itemColor, string? promptColor)
    {
        if (string.IsNullOrEmpty(itemColor) || string.IsNullOrEmpty(promptColor)) return false;
        if (itemColor.Contains(promptColor, StringComparison.OrdinalIgnoreCase)
            || promptColor.Contains(itemColor, StringComparison.OrdinalIgnoreCase))
            return true;

        var itemBasic = BasicColor(itemColor);
        return itemBasic != null && itemBasic == BasicColor(promptColor);
    }

    public static bool IsNeutral(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return false;
        // foreach (not LINQ .Any) to avoid a closure allocation on this per-candidate hot path
        foreach (var n in Neutrals)
            if (color.Contains(n, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    // hue family ("red", "blue", ...) for a non-neutral color, or null when neutral/unknown
    public static string? FamilyOf(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;
        if (IsNeutral(color)) return null;
        foreach (var (token, family) in FamilyTokens)
            if (color.Contains(token, StringComparison.OrdinalIgnoreCase)) return family;
        return null;
    }
}
