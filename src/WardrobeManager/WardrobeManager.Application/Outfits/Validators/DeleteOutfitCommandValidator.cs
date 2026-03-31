using FluentValidation;
using WardrobeManager.Application.Outfits.Commands;

namespace WardrobeManager.Application.Outfits.Validators;

public class DeleteOutfitCommandValidator : AbstractValidator<DeleteOutfitCommand>
{
    public DeleteOutfitCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
