using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Learning;

// a clothing item plus the decision the user made about it within one generation.
public readonly record struct ActionedItem(ClothingItem Item, FeedbackAction Action, int Rank);

internal static class FeedbackActions
{
    public static bool IsPositive(FeedbackAction action) =>
        action is FeedbackAction.Accepted or FeedbackAction.Worn or FeedbackAction.Favorited;

    // an item the generator put first but the user discarded — the strongest negative signal.
    public static bool IsActiveSwapOut(ActionedItem item) =>
        item.Action == FeedbackAction.Rejected && item.Rank == 0;
}
