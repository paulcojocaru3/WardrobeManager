using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Prompting;

public record PromptIntent
{
    public string? Style { get; init; }
    public string? City { get; init; }
    public string? Occasion { get; init; }

    // outfit-level colors (e.g. "an all-black outfit") — applied softly across every slot
    public IReadOnlyList<string> DesiredColors { get; init; } = new List<string>();
    public IReadOnlyList<string> AvoidColors { get; init; } = new List<string>();

    // colors bound to a specific garment (e.g. "black pants, non-black tee") — the LLM does the
    // color->garment association; applied as a hard filter on that slot only.
    public IReadOnlyList<GarmentSpec> GarmentSpecs { get; init; } = new List<GarmentSpec>();

    // a specific garment to build the outfit around
    public string? AnchorDescription { get; init; }
    public IReadOnlyList<ClothingType> RequestedTypes { get; init; } = new List<ClothingType>();
    public IReadOnlyList<RequestedGarment> RequestedGarments { get; init; } = new List<RequestedGarment>();

    public int? Formality { get; init; }

    // cold/mild/warm/hot — complements live weather data
    public string? TemperatureHint { get; init; }
}

public record RequestedGarment(string SubType, ClothingType Type);

public static class GarmentVocabulary
{
    private static readonly HashSet<string> Generic =
        new(StringComparer.OrdinalIgnoreCase) { "tops", "pants", "shoes" };

    public static bool IsGenericSubType(string? subType) =>
        !string.IsNullOrWhiteSpace(subType) && Generic.Contains(subType);
}
