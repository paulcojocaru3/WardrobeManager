using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing.Queries;
using WardrobeManager.Application.Outfits.Queries;

namespace WardrobeManager.Application.PlannedOutfits.Queries;

public record GetArchivedPlannerEventsQuery(Guid UserId) : IRequest<IEnumerable<PlannerEventDto>>;

public sealed class GetArchivedPlannerEventsQueryHandler : IRequestHandler<GetArchivedPlannerEventsQuery, IEnumerable<PlannerEventDto>>
{
    private readonly IPlannerEventRepository _plannerEventRepository;

    public GetArchivedPlannerEventsQueryHandler(IPlannerEventRepository plannerEventRepository)
    {
        _plannerEventRepository = plannerEventRepository;
    }

    public async Task<IEnumerable<PlannerEventDto>> Handle(GetArchivedPlannerEventsQuery request, CancellationToken cancellationToken)
    {
        var plannerEvents = (await _plannerEventRepository.GetByUserIdAsync(request.UserId, cancellationToken)).ToList();

        await PlannerEventProjection.ApplyLifecycleTransitionsAsync(plannerEvents, _plannerEventRepository, DateTime.UtcNow, cancellationToken);

        return plannerEvents.Where(p => p.Status == "Archived").Select(PlannerEventProjection.ToDto);
    }
}
