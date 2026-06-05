using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Prompting;

/// <summary>
/// Structured intent extracted from a free-text user prompt by the LLM.
/// Every field is optional so the system degrades gracefully when the prompt
/// (or the LLM) only provides partial information.
/// </summary>
public record PromptIntent
{
    /// <summary>One of the supported USAGES (Casual, Formal, Sports, ...), or null.</summary>
    public string? Style { get; init; }

    /// <summary>City mentioned in the prompt, used to fetch weather.</summary>
    public string? City { get; init; }

    /// <summary>Short free-text occasion (e.g. "wedding", "gym", "first date").</summary>
    public string? Occasion { get; init; }

    /// <summary>Colors the user explicitly wants.</summary>
    public IReadOnlyList<string> DesiredColors { get; init; } = new List<string>();

    /// <summary>Colors the user explicitly wants to avoid.</summary>
    public IReadOnlyList<string> AvoidColors { get; init; } = new List<string>();

    /// <summary>Description of a specific garment the user wants to build the outfit around.</summary>
    public string? AnchorDescription { get; init; }

    /// <summary>Clothing types the user explicitly requested.</summary>
    public IReadOnlyList<ClothingType> RequestedTypes { get; init; } = new List<ClothingType>();

    /// <summary>Formality level 1-5, or null.</summary>
    public int? Formality { get; init; }

    /// <summary>Temperature hint (cold/mild/warm/hot) complementing weather data.</summary>
    public string? TemperatureHint { get; init; }
}
