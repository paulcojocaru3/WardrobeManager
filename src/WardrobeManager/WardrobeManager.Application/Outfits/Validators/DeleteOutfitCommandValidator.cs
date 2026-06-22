using FluentValidation;
using WardrobeManager.Application.Outfits.Commands;

namespace WardrobeManager.Application.Outfits.Validators;

public sealed class DeleteOutfitCommandValidator : AbstractValidator<DeleteOutfitCommand>
{
    public DeleteOutfitCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
    }
}
