using System.Text.RegularExpressions;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Generation;

// Gemma chooses item numbers, while the database remains the source of truth for item attributes.
// Correct slot-specific colour phrases in generated prose before they reach the UI.
public static partial class StylistNarrativeGrounder
{
    // bounds each substitution so a pathological input can't hang the request (ReDoS guard).
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(200);

    private const string ColorPattern =
        "off[- ]white|navy blue|light blue|dark blue|light green|dark green|" +
        "black|white|gray|grey|charcoal|navy|blue|teal|turquoise|green|olive|sage|" +
        "khaki|beige|tan|camel|brown|cream|ivory|stone|ecru|yellow|mustard|orange|" +
        "rust|red|burgundy|maroon|pink|purple|violet";

    public static StylistOutfit Ground(StylistOutfit narrative, IReadOnlyCollection<ClothingItem> selectedItems)
    {
        return narrative with
        {
            Headline = GroundText(narrative.Headline, selectedItems),
            Highlights = narrative.Highlights
                .Select(text => GroundText(text, selectedItems))
                .ToList(),
            StylingTip = GroundText(narrative.StylingTip, selectedItems)
        };
    }

    private static string GroundText(string? text, IReadOnlyCollection<ClothingItem> selectedItems)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var grounded = text;
        foreach (var item in selectedItems)
        {
            var color = PrimaryColor(item.Color);
            if (color == null) continue;

            var garmentPattern = GarmentPattern(item.Type);
            if (garmentPattern == null) continue;

            var pattern = $@"\b(?<color>{ColorPattern})\b(?<bridge>(?:[\s-]+\w+){{0,2}}[\s-]+)(?<garment>{garmentPattern})\b";
            try
            {
                grounded = Regex.Replace(
                    grounded,
                    pattern,
                    match => MatchCase(color, match.Groups["color"].Value) +
                             match.Groups["bridge"].Value +
                             match.Groups["garment"].Value,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    RegexTimeout);
            }
            catch (RegexMatchTimeoutException)
            {
                // leave this item's colour ungrounded rather than fail the whole note.
            }
        }

        return grounded;
    }

    private static string? PrimaryColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;
        return color.Split([',', ';', '/', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
    }

    private static string? GarmentPattern(ClothingType type) => type switch
    {
        ClothingType.Top => "tops?|shirts?|t-?shirts?|tees?|blouses?|sweaters?|sweatshirts?|hoodies?",
        ClothingType.Bottom => "bottoms?|jeans?|denim|pants?|trousers?|shorts?|skirts?",
        ClothingType.Shoes => "shoes?|sneakers?|trainers?|boots?|sandals?|loafers?|heels?",
        ClothingType.Outerwear => "outerwear|jackets?|coats?|blazers?|parkas?",
        ClothingType.Accessory => "accessor(?:y|ies)|bags?|belts?|scarves?|hats?|caps?",
        _ => null
    };

    private static string MatchCase(string replacement, string original)
    {
        if (original.All(c => !char.IsLetter(c) || char.IsUpper(c)))
            return replacement.ToUpperInvariant();
        if (char.IsUpper(original[0]))
            return char.ToUpperInvariant(replacement[0]) + replacement[1..];
        return replacement.ToLowerInvariant();
    }
}
