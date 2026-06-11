using FluentValidation;
using WardrobeManager.Application.PlannedOutfits.Commands;

namespace WardrobeManager.Application.PlannedOutfits.Validators;

public sealed class ArchivePlannerEventCommandValidator : AbstractValidator<ArchivePlannerEventCommand>
{
    public ArchivePlannerEventCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.PlannerEventId)
            .NotEmpty();
    }
}
