using FluentValidation;
using WardrobeManager.Application.Clothing.Commands;

namespace WardrobeManager.Application.Clothing.Validators;

public sealed class DeleteClothingCommandValidator : AbstractValidator<DeleteClothingCommand>
{
    public DeleteClothingCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Clothing Id is required.");
    }
}
