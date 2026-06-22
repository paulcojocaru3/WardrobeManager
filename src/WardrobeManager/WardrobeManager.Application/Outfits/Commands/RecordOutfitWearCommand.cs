using MediatR;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Learning;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Commands;

public record RecordOutfitWearCommand(Guid UserId, Guid OutfitId) : IRequest<bool>;

public sealed class RecordOutfitWearCommandHandler : IRequestHandler<RecordOutfitWearCommand, bool>
{
    private readonly IOutfitRepository _outfitRepository;
    private readonly IWearEventRepository _wearEventRepository;
    private readonly IOutfitFeedbackRepository _feedbackRepository;
    private readonly IFeedbackLearningCoordinator _learningCoordinator;
    private readonly ILogger<RecordOutfitWearCommandHandler> _logger;
    private readonly TimeProvider _clock;

    public RecordOutfitWearCommandHandler(
        IOutfitRepository outfitRepository,
        IWearEventRepository wearEventRepository,
        IOutfitFeedbackRepository feedbackRepository,
        IFeedbackLearningCoordinator learningCoordinator,
        ILogger<RecordOutfitWearCommandHandler> logger,
        TimeProvider? clock = null)
    {
        _outfitRepository = outfitRepository;
        _wearEventRepository = wearEventRepository;
        _feedbackRepository = feedbackRepository;
        _learningCoordinator = learningCoordinator;
        _logger = logger;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<bool> Handle(RecordOutfitWearCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var today = now.Date;
        
        // count distinct OUTFIT wear events recorded today
        var eventsToday = await _wearEventRepository.GetByUserIdAsync(request.UserId, today, today.AddDays(1).AddTicks(-1), cancellationToken);
        
        // group by OutfitId AND a rounded timestamp (to 1 minute) to identify a "session"
        var distinctSessionsToday = eventsToday
            .Where(e => e.OutfitId.HasValue)
            .GroupBy(e => new { e.OutfitId, Time = e.WearDate.ToString("yyyy-MM-dd HH:mm") })
            .Count();

        if (distinctSessionsToday >= 10)
        {
            return false; // Limit reached
        }

        var outfit = await _outfitRepository.GetByIdAsync(request.OutfitId, cancellationToken);
        if (outfit == null || outfit.UserId != request.UserId)
        {
            return false;
        }

        var wearEvents = outfit.Items.Select(item =>
            WearEvent.RecordClothingWear(request.UserId, item.Id, outfit.Id, now));
        await _wearEventRepository.AddRangeAsync(wearEvents, cancellationToken);

        // wearing an AI outfit is the strongest positive signal — feed it back to the learners.
        await RecordWornFeedbackAsync(outfit, request.UserId, cancellationToken);

        return true;
    }

    private async Task RecordWornFeedbackAsync(Outfit outfit, Guid userId, CancellationToken ct)
    {
        if (outfit.AiGenerationId is not { } generationId) return;

        // best-effort: feedback/learning must never break recording a wear.
        try
        {
            var itemIds = outfit.Items.Select(i => i.Id).ToList();
            await _feedbackRepository.RecordActionsForItemsAsync(userId, generationId, itemIds, FeedbackAction.Worn, ct);
            await _learningCoordinator.LearnFromGenerationAsync(userId, generationId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Worn-feedback learning failed for outfit {OutfitId}.", outfit.Id);
        }
    }
}
