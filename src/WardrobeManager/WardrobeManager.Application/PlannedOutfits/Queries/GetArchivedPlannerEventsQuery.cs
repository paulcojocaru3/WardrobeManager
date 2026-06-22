using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing.Queries;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.PlannedOutfits.Queries;

public record GetArchivedPlannerEventsQuery(Guid UserId) : IRequest<IEnumerable<PlannerEventDto>>;

public sealed class GetArchivedPlannerEventsQueryHandler : IRequestHandler<GetArchivedPlannerEventsQuery, IEnumerable<PlannerEventDto>>
{
    private readonly IPlannerEventRepository _plannerEventRepository;
    private readonly TimeProvider _clock;

    public GetArchivedPlannerEventsQueryHandler(
        IPlannerEventRepository plannerEventRepository,
        TimeProvider? clock = null)
    {
        _plannerEventRepository = plannerEventRepository;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<IEnumerable<PlannerEventDto>> Handle(GetArchivedPlannerEventsQuery request, CancellationToken cancellationToken)
    {
        var plannerEvents = (await _plannerEventRepository.GetByUserIdAsync(request.UserId, cancellationToken)).ToList();

        await PlannerEventProjection.ApplyLifecycleTransitionsAsync(plannerEvents, _plannerEventRepository, _clock.GetUtcNow().UtcDateTime, cancellationToken);

        return plannerEvents.Where(p => p.Status == PlannerEvent.ArchivedStatus).Select(PlannerEventProjection.ToDto);
    }
}
