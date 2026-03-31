using FluentValidation;
using WardrobeManager.Application.Clothing.Commands;

namespace WardrobeManager.Application.Clothing.Validators;

public class AddClothingCommandValidator : AbstractValidator<AddClothingCommand>
{
    public AddClothingCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.ProcessedImageB64).NotEmpty();
    }
}
