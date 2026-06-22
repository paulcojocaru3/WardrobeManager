using MediatR;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Learning;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Commands;

public sealed class ToggleOutfitFavoriteCommandHandler(
    IOutfitRepository outfitRepository,
    IOutfitFeedbackRepository feedbackRepository,
    IFeedbackLearningCoordinator learningCoordinator,
    ILogger<ToggleOutfitFavoriteCommandHandler> logger) : IRequestHandler<ToggleOutfitFavoriteCommand, bool?>
{
    public async Task<bool?> Handle(ToggleOutfitFavoriteCommand request, CancellationToken ct)
    {
        var outfit = await outfitRepository.GetByIdForUserAsync(request.Id, request.UserId, ct);
        if (outfit == null)
        {
            return null;
        }

        var isFavorite = outfit.ToggleFavorite();
        await outfitRepository.UpdateAsync(outfit, ct);

        // favoriting an AI outfit is a positive signal; un-favoriting is not treated as a dislike.
        if (isFavorite && outfit.AiGenerationId is { } generationId)
        {
            try
            {
                var itemIds = outfit.Items.Select(i => i.Id).ToList();
                await feedbackRepository.RecordActionsForItemsAsync(outfit.UserId, generationId, itemIds, FeedbackAction.Favorited, ct);
                await learningCoordinator.LearnFromGenerationAsync(outfit.UserId, generationId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Favorited-feedback learning failed for outfit {OutfitId}.", outfit.Id);
            }
        }

        return isFavorite;
    }
}
