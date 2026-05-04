using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

public interface IOutfitEvaluator
{
    double Weight { get; }

    /// <summary>
    /// Evaluates the candidate item.
    /// Returns a score between -1.0 and 1.0.
    /// -1.0 means an absolute Veto (exclusion).
    /// </summary>
    double Evaluate(ClothingItem candidate, OutfitGenerationContext context);
}
