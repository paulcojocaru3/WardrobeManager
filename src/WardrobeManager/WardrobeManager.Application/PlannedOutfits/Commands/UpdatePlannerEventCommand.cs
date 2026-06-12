using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public record UpdatePlannerEventCommand(Guid UserId, Guid PlannerEventId, string Name, string Type, string Location, DateTime StartDate, DateTime EndDate, List<string> PreferredStyles) : IRequest<bool>;

public sealed class UpdatePlannerEventCommandHandler : IRequestHandler<UpdatePlannerEventCommand, bool>
{
    private readonly IPlannerEventRepository _plannerEventRepository;
    private readonly IValidator<UpdatePlannerEventCommand> _validator;

    public UpdatePlannerEventCommandHandler(
        IPlannerEventRepository plannerEventRepository,
        IValidator<UpdatePlannerEventCommand> validator)
    {
        _plannerEventRepository = plannerEventRepository;
        _validator = validator;
    }

    public async Task<bool> Handle(UpdatePlannerEventCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var plannerEvent = await _plannerEventRepository.GetByIdAsync(request.PlannerEventId, cancellationToken);
        if (plannerEvent == null || plannerEvent.UserId != request.UserId)
        {
            return false;
        }

        plannerEvent.Name = request.Name;
        plannerEvent.Type = request.Type;
        plannerEvent.Location = request.Location;
        plannerEvent.StartDate = request.StartDate.Date;
        plannerEvent.EndDate = request.EndDate.Date;
        if (request.PreferredStyles != null)
        {
            plannerEvent.PreferredStyles = request.PreferredStyles;
        }
        else
        {
            plannerEvent.PreferredStyles = new List<string>();
        }

        await _plannerEventRepository.UpdateAsync(plannerEvent, cancellationToken);

        return true;
    }
}
