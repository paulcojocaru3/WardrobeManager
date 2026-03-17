using FluentValidation;
using WardrobeManager.Application.Outfits.Commands;

namespace WardrobeManager.Application.Outfits.Validators;

public class GenerateOutfitCommandValidator : AbstractValidator<GenerateOutfitCommand>
{
    public GenerateOutfitCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.StartItemId)
            .NotEmpty().WithMessage("Start Item Id is required to generate an outfit.");
    }
}
