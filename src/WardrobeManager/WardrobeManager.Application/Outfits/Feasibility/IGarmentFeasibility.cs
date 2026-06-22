using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Feasibility;

// stage 1 of the two-stage model: decides whether a candidate may fill a slot (hard, relaxable
public interface IGarmentFeasibility
{
    // the hard constraints this candidate violates in the given context (empty => fully feasible).
    FeasibilityResult Check(ClothingItem item, OutfitGenerationContext context);

    // picks the slot's feasible pool by relaxing constraints in priority order (ConstraintKind order):
    IReadOnlyList<FeasibleCandidate> FilterWithRelaxation(
        IReadOnlyList<(ClothingItem Item, double Similarity)> candidates,
        OutfitGenerationContext context);
}
