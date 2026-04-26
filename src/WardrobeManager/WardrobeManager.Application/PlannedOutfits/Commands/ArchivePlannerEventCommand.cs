using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public record ArchivePlannerEventCommand(Guid UserId, Guid PlannerEventId) : IRequest<bool>;

public class ArchivePlannerEventCommandValidator : AbstractValidator<ArchivePlannerEventCommand>
{
    public ArchivePlannerEventCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.PlannerEventId)
            .NotEmpty();
    }
}

public class ArchivePlannerEventCommandHandler : IRequestHandler<ArchivePlannerEventCommand, bool>
{
    private readonly IPlannerEventRepository _plannerEventRepository;
    private readonly IValidator<ArchivePlannerEventCommand> _validator;

    public ArchivePlannerEventCommandHandler(
        IPlannerEventRepository plannerEventRepository,
        IValidator<ArchivePlannerEventCommand> validator)
    {
        _plannerEventRepository = plannerEventRepository;
        _validator = validator;
    }

    public async Task<bool> Handle(ArchivePlannerEventCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var plannerEvent = await _plannerEventRepository.GetByIdAsync(request.PlannerEventId, cancellationToken);
        if (plannerEvent == null || plannerEvent.UserId != request.UserId)
        {
            return false;
        }

        plannerEvent.Status = "Archived";
        plannerEvent.ArchivedAt = DateTime.UtcNow;

        await _plannerEventRepository.UpdateAsync(plannerEvent, cancellationToken);
        return true;
    }
}
