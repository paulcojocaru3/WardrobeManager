using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public record ArchivePlannerEventCommand(Guid UserId, Guid PlannerEventId) : IRequest<bool>;

public sealed class ArchivePlannerEventCommandHandler : IRequestHandler<ArchivePlannerEventCommand, bool>
{
    private readonly IPlannerEventRepository _plannerEventRepository;
    private readonly TimeProvider _clock;

    public ArchivePlannerEventCommandHandler(
        IPlannerEventRepository plannerEventRepository,
        TimeProvider? clock = null)
    {
        _plannerEventRepository = plannerEventRepository;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<bool> Handle(ArchivePlannerEventCommand request, CancellationToken cancellationToken)
    {
        var plannerEvent = await _plannerEventRepository.GetByIdAsync(request.PlannerEventId, cancellationToken);
        if (plannerEvent == null || plannerEvent.UserId != request.UserId)
        {
            return false;
        }

        plannerEvent.Archive(_clock.GetUtcNow().UtcDateTime);

        await _plannerEventRepository.UpdateAsync(plannerEvent, cancellationToken);
        return true;
    }
}
