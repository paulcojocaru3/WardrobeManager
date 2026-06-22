using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Scoring;

// stage 2 of generation: a soft, graded preference signal. Hard feasibility (vetoes) lives in
public interface IOutfitEvaluator
{
    // stable identifier. Evaluator weights are intentionally static (behaviour learning is layered
    string Name { get; }

    // soft score as a multiplier in [0.05, 1.5]. 1.0 is neutral. A value near 0 acts as a soft veto.
    double Evaluate(ClothingItem candidate, OutfitGenerationContext context);
}
