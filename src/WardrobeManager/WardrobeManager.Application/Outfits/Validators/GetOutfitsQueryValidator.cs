using FluentValidation;
using WardrobeManager.Application.Outfits.Queries;

namespace WardrobeManager.Application.Outfits.Validators;

public class GetOutfitsQueryValidator : AbstractValidator<GetOutfitsQuery>
{
    public GetOutfitsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required to fetch outfits.");
    }
}
