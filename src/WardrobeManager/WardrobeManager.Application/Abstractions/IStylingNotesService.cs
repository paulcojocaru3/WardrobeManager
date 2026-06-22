using WardrobeManager.Application.Outfits.Explaining;

namespace WardrobeManager.Application.Abstractions;

// explain selected outfits with local templates.
public interface IStylingNotesService
{
    Task<IReadOnlyList<string>> GenerateAsync(OutfitExplanation explanation, CancellationToken ct = default);

    // build structured insight for the daily outfit.
    Task<OutfitInsight> GenerateInsightAsync(OutfitExplanation explanation, CancellationToken ct = default);
}
