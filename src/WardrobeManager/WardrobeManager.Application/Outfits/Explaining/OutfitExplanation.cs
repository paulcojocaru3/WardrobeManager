using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Outfits.Explaining;

// structured, grounded facts about an already-chosen outfit. This is the ONLY input the styling-notes
public sealed record OutfitExplanation
{
    public string? Style { get; init; }
    public string? Occasion { get; init; }
    public WeatherData? Weather { get; init; }
    public IReadOnlyList<OutfitPieceFact> Pieces { get; init; } = new List<OutfitPieceFact>();

    // human-readable trade-offs the generator had to make (the relaxation warnings)
    public IReadOnlyList<string> Tradeoffs { get; init; } = new List<string>();
}

public sealed record OutfitPieceFact
{
    public Guid ItemId { get; init; }          // lets per-item insight notes map back to the garment
    public string Slot { get; init; } = "";   // top / bottom / shoes / ...
    public string Name { get; init; } = "";
    public string? Color { get; init; }
    public string? Material { get; init; }
    public string? Season { get; init; }
    public string? SubType { get; init; }
    public string? Style { get; init; }

    // canonical garment phrase composed from the CLIP-derived attributes (the "embedding text"), e.g.
    public string? Description { get; init; }

    // short factual highlights derived from the evaluator scores (e.g. "coordinates with the palette")
    public IReadOnlyList<string> Highlights { get; init; } = new List<string>();
}

// richer, structured insight for the Outfit-of-the-day card: a one-line idea, a note per garment, and
public sealed record OutfitInsight
{
    public string Headline { get; init; } = "";
    public IReadOnlyList<OutfitItemNote> Items { get; init; } = new List<OutfitItemNote>();
    public string? WeatherAdvice { get; init; }

    // practical "how to wear it" styling tips (tuck, cuff, roll, layer, proportion).
    public IReadOnlyList<string> Tips { get; init; } = new List<string>();

    // a couple of free-form supporting notes (kept for parity with the flat styling-notes feature)
    public IReadOnlyList<string> Notes { get; init; } = new List<string>();
}

public sealed record OutfitItemNote(Guid ItemId, string Slot, string Note);
