namespace WardrobeManager.Application.Outfits.Scoring;

// normalizes an item's color/style into the stable keys used by the learned taste profile.
public static class TasteKey
{
    // basic color word ("navy" -> "blue", "charcoal" -> "black"), else the raw color lowercased.
    public static string? Color(string? color)
    {
        var basic = ColorFamily.BasicColor(color);
        if (basic != null) return basic;
        return string.IsNullOrWhiteSpace(color) ? null : color.Trim().ToLowerInvariant();
    }

    // the item's primary style: its first Usage tag, lowercased.
    public static string? Style(string? usage)
    {
        if (string.IsNullOrWhiteSpace(usage)) return null;
        var first = usage.Split(',')[0].Trim().ToLowerInvariant();
        return first.Length == 0 ? null : first;
    }
}
