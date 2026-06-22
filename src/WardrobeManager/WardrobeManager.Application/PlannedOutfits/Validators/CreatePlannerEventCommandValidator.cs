using FluentValidation;
using WardrobeManager.Application.PlannedOutfits.Commands;

namespace WardrobeManager.Application.PlannedOutfits.Validators;

public sealed class CreatePlannerEventCommandValidator : AbstractValidator<CreatePlannerEventCommand>
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

        RuleFor(x => x.ReuseAfterDays)
            .GreaterThanOrEqualTo(2)
            .When(x => x.ReuseAfterDays.HasValue)
            .WithMessage("Reuse interval must be at least 2 days.");
    }
}
