using FluentValidation;
using WardrobeManager.Application.Clothing.Commands;

namespace WardrobeManager.Application.Clothing.Validators;

public class DeleteClothingCommandValidator : AbstractValidator<DeleteClothingCommand>
{
    public DeleteClothingCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Clothing Id is required.");
    }
}
