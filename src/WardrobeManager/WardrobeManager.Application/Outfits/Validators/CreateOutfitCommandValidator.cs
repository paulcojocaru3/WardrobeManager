using FluentValidation;
using WardrobeManager.Application.Outfits.Commands;

namespace WardrobeManager.Application.Outfits.Validators;

public sealed class CreateOutfitCommandValidator : AbstractValidator<CreateOutfitCommand>
{
    public CreateOutfitCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ItemIds).NotEmpty().Must(x => x.Count > 0).WithMessage("An outfit must contain at least one item.");
    }
}
