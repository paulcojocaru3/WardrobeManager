using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public record CreatePlannerEventCommand(Guid UserId, string Name, string Type, string Location, DateTime StartDate, DateTime EndDate, List<string> PreferredStyles) : IRequest<Guid>;

public class CreatePlannerEventCommandValidator : AbstractValidator<CreatePlannerEventCommand>
{
    public CreatePlannerEventCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Type)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Location)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.EndDate.Date)
            .GreaterThanOrEqualTo(x => x.StartDate.Date)
            .WithMessage("End date must be greater than or equal to start date.");
    }
}

public class CreatePlannerEventCommandHandler : IRequestHandler<CreatePlannerEventCommand, Guid>
{
    private readonly IPlannerEventRepository _plannerEventRepository;
    private readonly IValidator<CreatePlannerEventCommand> _validator;

    public CreatePlannerEventCommandHandler(
        IPlannerEventRepository plannerEventRepository,
        IValidator<CreatePlannerEventCommand> validator)
    {
        _plannerEventRepository = plannerEventRepository;
        _validator = validator;
    }

    public async Task<Guid> Handle(CreatePlannerEventCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var plannerEvent = new PlannerEvent
        {
            UserId = request.UserId,
            Name = request.Name,
            Type = request.Type,
            Location = request.Location,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate.Date,
            Status = "Active", // Set default status to Active
            PreferredStyles = request.PreferredStyles ?? new List<string>()
        };

        await _plannerEventRepository.AddAsync(plannerEvent, cancellationToken);

        return plannerEvent.Id;
    }
}
