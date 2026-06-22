using FluentValidation;
using WardrobeManager.Application.PlannedOutfits.Commands;
using WardrobeManager.Application.PlannedOutfits.Queries;

namespace WardrobeManager.Application.PlannedOutfits.Validators;

public sealed class GetPlannerEventsQueryValidator : AbstractValidator<GetPlannerEventsQuery>
{
    public GetPlannerEventsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class GetArchivedPlannerEventsQueryValidator : AbstractValidator<GetArchivedPlannerEventsQuery>
{
    public GetArchivedPlannerEventsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class AddEventItineraryCommandValidator : AbstractValidator<AddEventItineraryCommand>
{
    public AddEventItineraryCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PlannerEventId).NotEmpty();
        RuleFor(x => x.OutfitId).NotEmpty();
        RuleFor(x => x.Moment).NotEmpty().MaximumLength(80);
    }
}

public sealed class UpdateEventItineraryCommandValidator : AbstractValidator<UpdateEventItineraryCommand>
{
    public UpdateEventItineraryCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PlannerEventId).NotEmpty();
        RuleFor(x => x.ItineraryId).NotEmpty();
        RuleFor(x => x.OutfitId).NotEmpty();
        RuleFor(x => x.Moment).NotEmpty().MaximumLength(80);
    }
}

public sealed class DeleteEventItineraryCommandValidator : AbstractValidator<DeleteEventItineraryCommand>
{
    public DeleteEventItineraryCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PlannerEventId).NotEmpty();
        RuleFor(x => x.ItineraryId).NotEmpty();
    }
}

public sealed class DeletePlannerEventCommandValidator : AbstractValidator<DeletePlannerEventCommand>
{
    public DeletePlannerEventCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PlannerEventId).NotEmpty();
    }
}

public sealed class GenerateEventOutfitsCommandValidator : AbstractValidator<GenerateEventOutfitsCommand>
{
    public GenerateEventOutfitsCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PlannerEventId).NotEmpty();
    }
}

public sealed class RegenerateEventItineraryOutfitCommandValidator : AbstractValidator<RegenerateEventItineraryOutfitCommand>
{
    public RegenerateEventItineraryOutfitCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PlannerEventId).NotEmpty();
        RuleFor(x => x.ItineraryId).NotEmpty();
    }
}

public sealed class CheckWeatherAlertsCommandValidator : AbstractValidator<CheckWeatherAlertsCommand>
{
}
