using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public record UpdatePlannerEventCommand(Guid UserId, Guid PlannerEventId, string Name, string Type, string Location, DateTime StartDate, DateTime EndDate, List<string> PreferredStyles) : IRequest<bool>;

public class UpdatePlannerEventCommandValidator : AbstractValidator<UpdatePlannerEventCommand>
{
    public UpdatePlannerEventCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PlannerEventId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(120);
        RuleFor(x => x.EndDate.Date)
            .GreaterThanOrEqualTo(x => x.StartDate.Date)
            .WithMessage("End date must be greater than or equal to start date.");
    }
}

public class UpdatePlannerEventCommandHandler : IRequestHandler<UpdatePlannerEventCommand, bool>
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
        plannerEvent.PreferredStyles = request.PreferredStyles ?? new List<string>();

        await _plannerEventRepository.UpdateAsync(plannerEvent, cancellationToken);

        return true;
    }
}
