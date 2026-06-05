namespace WardrobeManager.Application.Abstractions;

/// <summary>
/// Deterministic occasion -> style classification from a curated bilingual keyword map.
/// Returns one of the USAGES (Casual, Formal, Sports, ...), or null when no occasion
/// keyword is found. This is the PRIMARY style signal — the LLM is only a fallback for
/// style, so the closed style taxonomy is mapped reliably and for free.
/// </summary>
public interface IOccasionClassifier
{
    string? ClassifyStyle(string prompt);
}
