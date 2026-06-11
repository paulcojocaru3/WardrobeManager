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
    IUserEvaluatorWeightsRepository weightsRepository,
    IWeightLearningService weightLearningService,
    ILogger<RecordOutfitFeedbackCommandHandler> logger) : IRequestHandler<RecordOutfitFeedbackCommand, bool>
{
    private const int RetrainEvery = 8; // retrain once this many new labels accumulate

    public async Task<bool> Handle(RecordOutfitFeedbackCommand request, CancellationToken ct)
    {
        foreach (var item in request.Items)
        {
            if (Enum.TryParse<FeedbackAction>(item.Action, ignoreCase: true, out var action) && action != FeedbackAction.Shown)
                await feedbackRepository.RecordActionAsync(request.UserId, request.GenerationId, item.ClothingItemId, action, ct);
        }

        var labeled = await feedbackRepository.CountActionableAsync(request.UserId, ct);
        var weights = await weightsRepository.GetByUserIdAsync(request.UserId, ct);
        int trainedOn;
        if (weights != null)
        {
            trainedOn = weights.TrainedOnSamples;
        }
        else
        {
            trainedOn = 0;
        }

        if (labeled - trainedOn >= RetrainEvery)
        {
            // Best-effort: a training failure must never break feedback recording.
            try { await weightLearningService.RetrainAsync(request.UserId, ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Weight retraining failed for user {UserId}.", request.UserId);
            }
        }

        return true;
    }
}
