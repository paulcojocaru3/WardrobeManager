using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public record DeletePlannerEventCommand(Guid UserId, Guid PlannerEventId) : IRequest<bool>;

public sealed class DeletePlannerEventCommandHandler : IRequestHandler<DeletePlannerEventCommand, bool>
{
    private readonly IPlannerEventRepository _plannerEventRepository;

    public DeletePlannerEventCommandHandler(IPlannerEventRepository plannerEventRepository)
    {
        _plannerEventRepository = plannerEventRepository;
    }

    public async Task<bool> Handle(DeletePlannerEventCommand request, CancellationToken cancellationToken)
    {
        var plannerEvent = await _plannerEventRepository.GetByIdAsync(request.PlannerEventId, cancellationToken);
        if (plannerEvent == null || plannerEvent.UserId != request.UserId)
        {
            return false;
        }

        await _plannerEventRepository.DeleteAsync(plannerEvent, cancellationToken);
        return true;
    }
}