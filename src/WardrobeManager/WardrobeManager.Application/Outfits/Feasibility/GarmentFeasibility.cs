using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Feasibility;

// the single hard-constraint authority. Pure logic (no I/O); the temperature thresholds and warm-only
public sealed class GarmentFeasibility(IThermalRules thermal) : IGarmentFeasibility
{
    // relaxation levels to try, lowest first: at level L any constraint with value < L may be violated.
    private static readonly ConstraintKind[] OrderedKinds =
        Enum.GetValues<ConstraintKind>().OrderBy(k => (int)k).ToArray();

    public FeasibilityResult Check(ClothingItem item, OutfitGenerationContext context)
    {
        var violations = new HashSet<ConstraintKind>();

        context.GarmentConstraints.TryGetValue(item.Type, out var spec);

        // subtype: the slot asked for a specific article type (e.g. "jeans").
        if (spec != null && !string.IsNullOrWhiteSpace(spec.SubType) &&
            !string.Equals(item.SubType, spec.SubType, StringComparison.OrdinalIgnoreCase))
        {
            violations.Add(ConstraintKind.SubType);
        }

        // desiredcolor: per-slot desired colors take precedence over the outfit-level ones.
        var desired = spec is { DesiredColors.Count: > 0 } ? spec.DesiredColors : context.DesiredColors;
        if (desired.Count > 0 && !string.IsNullOrEmpty(item.Color) &&
            !desired.Any(d => ColorFamily.ColorsMatch(item.Color, d)))
        {
            violations.Add(ConstraintKind.DesiredColor);
        }

        // style: only a *hard* clash is a constraint; graded style fit stays in StyleEvaluator.
        if (!string.IsNullOrEmpty(context.TargetStyle) && !string.IsNullOrEmpty(item.Usage) &&
            IsHardStyleMismatch(context.TargetStyle!, item.Usage!))
        {
            violations.Add(ConstraintKind.Style);
        }

        // avoidcolor: per-slot avoid + outfit-level avoid (soft favourite-avoid stays in scoring).
        if (!string.IsNullOrEmpty(item.Color) && IsAvoided(item.Color!, spec, context))
        {
            violations.Add(ConstraintKind.AvoidColor);
        }

        // gender: the seed locks the outfit's gender; Unisex/unknown items never violate it.
        if (!string.IsNullOrEmpty(context.TargetGender) && !string.IsNullOrEmpty(item.Gender) &&
            !item.Gender!.Equals("Unisex", StringComparison.OrdinalIgnoreCase) &&
            !item.Gender.Equals(context.TargetGender, StringComparison.OrdinalIgnoreCase))
        {
            violations.Add(ConstraintKind.Gender);
        }

        // weather: unwearable for the conditions (comfort).
        if (context.Weather != null && thermal.IsWeatherVetoed(item, context.Weather))
        {
            violations.Add(ConstraintKind.Weather);
        }

        return violations.Count == 0 ? FeasibilityResult.Feasible : new FeasibilityResult(violations);
    }

    public IReadOnlyList<FeasibleCandidate> FilterWithRelaxation(
        IReadOnlyList<(ClothingItem Item, double Similarity)> candidates,
        OutfitGenerationContext context)
    {
        if (candidates.Count == 0) return Array.Empty<FeasibleCandidate>();

        var assessed = candidates
            .Select(c => (c.Item, c.Similarity, Violations: Check(c.Item, context).Violations))
            .ToList();

        // try increasing relaxation: level 0 = satisfy everything, level N = allow violating the N
        for (var level = 0; level <= OrderedKinds.Length; level++)
        {
            var allowed = OrderedKinds.Where(k => (int)k < level).ToHashSet();
            var kept = assessed
                .Where(a => a.Violations.IsSubsetOf(allowed))
                .Select(a => new FeasibleCandidate(a.Item, a.Similarity, a.Violations))
                .ToList();
            if (kept.Count > 0) return kept;
        }

        return Array.Empty<FeasibleCandidate>();
    }

    private static bool IsAvoided(string color, Generation.GarmentSpec? spec, OutfitGenerationContext context)
    {
        if (spec is { AvoidColors.Count: > 0 } &&
            spec.AvoidColors.Any(a => ColorFamily.ColorsMatch(color, a)))
        {
            return true;
        }
        return context.AvoidColors.Count > 0 &&
               context.AvoidColors.Any(a => ColorFamily.ColorsMatch(color, a));
    }

    // pairings that never read as deliberate, regardless of degree (mirrors the old veto table that was
    public static bool IsHardStyleMismatch(string target, string usage)
    {
        bool U(string s) => usage.Contains(s, StringComparison.OrdinalIgnoreCase);
        if (target.Equals("Formal", StringComparison.OrdinalIgnoreCase)) return U("Sports") || U("Lounge");
        if (target.Equals("Sports", StringComparison.OrdinalIgnoreCase)) return U("Formal") || U("Party");
        if (target.Equals("Party", StringComparison.OrdinalIgnoreCase)) return U("Sports");
        return false;
    }
}
