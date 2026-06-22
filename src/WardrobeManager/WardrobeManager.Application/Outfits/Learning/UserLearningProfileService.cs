using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Learning;

public sealed class UserLearningProfileService(
    IUserLearningProfileRepository profileRepository,
    ILogger<UserLearningProfileService> logger,
    TimeProvider? clock = null)
{
    private const double Alpha = 0.15;
    private const double NeutralPrior = 0.5;

    public async Task UpdateAsync(Guid userId, string? occasion, IReadOnlyList<ActionedItem> actioned, CancellationToken ct = default)
    {
        var signals = actioned
            .Select(a => (a.Item, Target: TargetFor(a)))
            .Where(x => x.Target.HasValue)
            .Select(x => (x.Item, Target: x.Target!.Value))
            .ToList();
        if (signals.Count == 0) return;

        var global = await profileRepository.GetByUserIdAsync(userId, ct)
                     ?? new UserLearningProfile { UserId = userId };
        Apply(global.ColorScores, global.StyleScores, signals);
        global.UpdatedAt = (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        await profileRepository.UpsertAsync(global, ct);

        logger.LogInformation("Updated taste profile for user {UserId} (occasion {Occasion}) from {Count} signals.",
            userId, occasion ?? "global", signals.Count);
    }

    private static void Apply(
        Dictionary<string, double> colors, Dictionary<string, double> styles,
        IReadOnlyList<(ClothingItem Item, double Target)> signals)
    {
        foreach (var (item, target) in signals)
        {
            var colorKey = TasteKey.Color(item.Color);
            if (colorKey != null) Nudge(colors, colorKey, target);

            var styleKey = TasteKey.Style(item.Usage);
            if (styleKey != null) Nudge(styles, styleKey, target);
        }
    }

    private static double? TargetFor(ActionedItem a)
    {
        if (FeedbackActions.IsPositive(a.Action)) return 1.0;
        if (FeedbackActions.IsActiveSwapOut(a)) return 0.0;
        return null;
    }

    private static void Nudge(Dictionary<string, double> scores, string key, double target)
    {
        var current = scores.TryGetValue(key, out var v) ? v : NeutralPrior;
        scores[key] = Alpha * target + (1.0 - Alpha) * current;
    }
}
