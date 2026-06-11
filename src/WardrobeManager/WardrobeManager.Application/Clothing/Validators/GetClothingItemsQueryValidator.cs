using FluentValidation;
using WardrobeManager.Application.Clothing.Queries;

namespace WardrobeManager.Application.Clothing.Validators;

public sealed class GetClothingItemsQueryValidator : AbstractValidator<GetClothingItemsQuery>
{
    public GetClothingItemsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required to fetch wardrobe items.");
    }
}
