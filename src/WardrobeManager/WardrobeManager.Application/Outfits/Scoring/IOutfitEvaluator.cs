using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

public interface IOutfitEvaluator
{
    // stable key for looking up a learned weight (falls back to Weight)
    string Name { get; }

    double Weight { get; }

    // score in [-1.0, 1.0]; null abstains (excluded from the weighted average), -1.0 is a hard veto
    double? Evaluate(ClothingItem candidate, OutfitGenerationContext context);
}
