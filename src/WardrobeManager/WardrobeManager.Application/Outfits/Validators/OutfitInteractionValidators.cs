using FluentValidation;
using WardrobeManager.Application.Outfits.Commands;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Validators;

public sealed class ToggleOutfitFavoriteCommandValidator : AbstractValidator<ToggleOutfitFavoriteCommand>
{
    public ToggleOutfitFavoriteCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class RecordOutfitWearCommandValidator : AbstractValidator<RecordOutfitWearCommand>
{
    public RecordOutfitWearCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.OutfitId).NotEmpty();
    }
}

public sealed class RecordOutfitFeedbackCommandValidator : AbstractValidator<RecordOutfitFeedbackCommand>
{
    public RecordOutfitFeedbackCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.GenerationId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ClothingItemId).NotEmpty();
            item.RuleFor(x => x.Action)
                .NotEmpty()
                .Must(action => Enum.TryParse<FeedbackAction>(action, ignoreCase: true, out _))
                .WithMessage("Feedback action is not supported.");
        });
    }
}

public sealed class GetLearnedProfileQueryValidator : AbstractValidator<GetLearnedProfileQuery>
{
    public GetLearnedProfileQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class ExplainOutfitQueryValidator : AbstractValidator<ExplainOutfitQuery>
{
    public ExplainOutfitQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ItemIds).NotEmpty();
        RuleFor(x => x.Style).MaximumLength(80);
        RuleFor(x => x.Occasion).MaximumLength(120);
        RuleFor(x => x.City).MaximumLength(120);
    }
}

public sealed class OutfitInsightQueryValidator : AbstractValidator<OutfitInsightQuery>
{
    public OutfitInsightQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ItemIds).NotEmpty();
        RuleFor(x => x.Style).MaximumLength(80);
        RuleFor(x => x.Occasion).MaximumLength(120);
        RuleFor(x => x.City).MaximumLength(120);
    }
}
