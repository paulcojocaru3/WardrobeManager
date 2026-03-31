using FluentValidation;
using WardrobeManager.Application.Outfits.Commands;

namespace WardrobeManager.Application.Outfits.Validators;

public class GenerateAiOutfitCommandValidator : AbstractValidator<GenerateAiOutfitCommand>
{
    public GenerateAiOutfitCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.StartItemId).NotEmpty();
        RuleFor(x => x.Threshold).InclusiveBetween(0, 1);
    }
}
