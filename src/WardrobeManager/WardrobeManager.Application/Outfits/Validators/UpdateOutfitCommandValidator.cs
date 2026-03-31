using FluentValidation;
using WardrobeManager.Application.Outfits.Commands;

namespace WardrobeManager.Application.Outfits.Validators;

public class UpdateOutfitCommandValidator : AbstractValidator<UpdateOutfitCommand>
{
    public UpdateOutfitCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ItemIds).NotEmpty().Must(x => x.Count > 0).WithMessage("An outfit must contain at least one item.");
    }
}
