using FluentValidation;
using WardrobeManager.Application.Clothing.Commands;

namespace WardrobeManager.Application.Clothing.Validators;

public sealed class ProcessClothingCommandValidator : AbstractValidator<ProcessClothingCommand>
{
    public ProcessClothingCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FileContent).NotEmpty();
    }
}
