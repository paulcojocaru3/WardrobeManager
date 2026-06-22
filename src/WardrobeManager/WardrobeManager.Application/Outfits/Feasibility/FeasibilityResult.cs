using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Feasibility;

// the hard constraints a candidate violates in a given context. Empty => fully feasible.
public sealed class FeasibilityResult
{
    public static readonly FeasibilityResult Feasible = new(new HashSet<ConstraintKind>());

    public FeasibilityResult(IReadOnlySet<ConstraintKind> violations) => Violations = violations;

    public IReadOnlySet<ConstraintKind> Violations { get; }
    public bool IsFeasible => Violations.Count == 0;
}

// a retrieved candidate paired with its raw similarity and the constraints it had to relax to stay in
public sealed record FeasibleCandidate(
    ClothingItem Item,
    double Similarity,
    IReadOnlySet<ConstraintKind> Relaxed);
