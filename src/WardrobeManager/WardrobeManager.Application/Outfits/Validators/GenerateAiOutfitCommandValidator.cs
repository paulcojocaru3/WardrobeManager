using FluentValidation;
using WardrobeManager.Application.Outfits.Commands;

namespace WardrobeManager.Application.Outfits.Validators;

public sealed class GenerateAiOutfitCommandValidator : AbstractValidator<GenerateAiOutfitCommand>
{
    public GenerateAiOutfitCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        // require a seed unless rediscover mode chooses one.
        RuleFor(x => x.StartItemId).NotNull().NotEqual(Guid.Empty).When(x => !x.AnchorOnUnused);
        RuleFor(x => x.Threshold).InclusiveBetween(0, 1);
    }
}
