using FluentValidation;
using WardrobeManager.Application.Clothing.Commands;
using WardrobeManager.Application.Clothing.Queries;

namespace WardrobeManager.Application.Clothing.Validators;

public sealed class UpdateClothingCommandValidator : AbstractValidator<UpdateClothingCommand>
{
    public UpdateClothingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SubType).MaximumLength(80);
        RuleFor(x => x.Color).MaximumLength(40);
        RuleFor(x => x.Gender).MaximumLength(40);
        RuleFor(x => x.Season).MaximumLength(40);
        RuleFor(x => x.Usage).MaximumLength(80);
    }
}

public sealed class FindSimilarItemsQueryValidator : AbstractValidator<FindSimilarItemsQuery>
{
    public FindSimilarItemsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Limit).InclusiveBetween(1, 50);
    }
}

public sealed class GetArticleSubtypesQueryValidator : AbstractValidator<GetArticleSubtypesQuery>
{
}
