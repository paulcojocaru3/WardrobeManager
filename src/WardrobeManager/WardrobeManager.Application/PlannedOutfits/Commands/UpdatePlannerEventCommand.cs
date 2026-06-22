using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public record UpdatePlannerEventCommand(Guid UserId, Guid PlannerEventId, string Name, string Type, string Location, DateTime StartDate, DateTime EndDate, List<string> PreferredStyles, int? ReuseAfterDays = null) : IRequest<bool>;

public sealed class UpdatePlannerEventCommandHandler : IRequestHandler<UpdatePlannerEventCommand, bool>
{
    private readonly IPlannerEventRepository _plannerEventRepository;

    public UpdatePlannerEventCommandHandler(IPlannerEventRepository plannerEventRepository)
    {
        _plannerEventRepository = plannerEventRepository;
    }

    public async Task<bool> Handle(UpdatePlannerEventCommand request, CancellationToken cancellationToken)
    {
        var plannerEvent = await _plannerEventRepository.GetByIdAsync(request.PlannerEventId, cancellationToken);
        if (plannerEvent == null || plannerEvent.UserId != request.UserId)
        {
            return false;
        }

        plannerEvent.UpdateDetails(
            request.Name,
            request.Type,
            request.Location,
            request.StartDate,
            request.EndDate,
            request.PreferredStyles,
            request.ReuseAfterDays);

        await _plannerEventRepository.UpdateAsync(plannerEvent, cancellationToken);

        return true;
    }
}
