namespace WardrobeManager.Application.Abstractions;

public sealed record StylistItem(int Number, string Slot, string Line);

public sealed record StylistContext(
    string? Occasion,
    string? TimeOfDay,
    string? WeatherSummary,
    bool AllowOuterwear = true,
    string? Style = null,
    int? MandatoryItemNumber = null,
    string? MandatorySlot = null,
    bool Shuffle = false,
    IReadOnlyList<string>? FavoriteColors = null,
    IReadOnlyList<string>? AvoidColors = null);

public sealed record StylistOutfit(
    IReadOnlyList<int> ItemNumbers,
    string Headline,
    IReadOnlyList<string> Highlights,
    string StylingTip);

// let gemma3 compose outfits from validated fashionclip candidates.
public interface IOutfitStylist
{
    Task<IReadOnlyList<StylistOutfit>?> ComposeAsync(
        IReadOnlyList<StylistItem> candidates, StylistContext context, CancellationToken ct = default);

    Task<IReadOnlyList<StylistOutfit>?> RepairAsync(
        IReadOnlyList<StylistItem> candidates,
        StylistContext context,
        IReadOnlyList<StylistOutfit> invalidOutfits,
        string validationError,
        CancellationToken ct = default);
}
