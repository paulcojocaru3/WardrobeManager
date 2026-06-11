using FluentValidation;
using WardrobeManager.Application.Outfits.Commands;

namespace WardrobeManager.Application.Outfits.Validators;

public sealed class GenerateOutfitFromPromptCommandValidator : AbstractValidator<GenerateOutfitFromPromptCommand>
{
    public GenerateOutfitFromPromptCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Prompt).NotEmpty().MaximumLength(1000);
    }
}
