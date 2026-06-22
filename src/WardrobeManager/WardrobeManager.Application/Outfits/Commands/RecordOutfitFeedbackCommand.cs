using MediatR;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Learning;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Commands;

public record OutfitFeedbackItem(Guid ClothingItemId, string Action);

public record RecordOutfitFeedbackCommand(Guid UserId, Guid GenerationId, List<OutfitFeedbackItem> Items) : IRequest<bool>;

public sealed class RecordOutfitFeedbackCommandHandler(
    IOutfitFeedbackRepository feedbackRepository,
    IFeedbackLearningCoordinator learningCoordinator,
    ILogger<RecordOutfitFeedbackCommandHandler> logger) : IRequestHandler<RecordOutfitFeedbackCommand, bool>
{
    public async Task<bool> Handle(RecordOutfitFeedbackCommand request, CancellationToken ct)
    {
        foreach (var item in request.Items)
        {
            if (Enum.TryParse<FeedbackAction>(item.Action, ignoreCase: true, out var action) && action != FeedbackAction.Shown)
                await feedbackRepository.RecordActionAsync(request.UserId, request.GenerationId, item.ClothingItemId, action, ct);
        }

        // best-effort: a learning failure must never break feedback recording.
        try { await learningCoordinator.LearnFromGenerationAsync(request.UserId, request.GenerationId, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Behaviour learning failed for user {UserId}.", request.UserId);
        }

        return true;
    }
}
