using FluentValidation;
using WardrobeManager.Application.Clothing.Commands;

namespace WardrobeManager.Application.Clothing.Validators;

public class UploadClothingCommandValidator : AbstractValidator<UploadClothingCommand>
{
    public UploadClothingCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(50).WithMessage("Name must not exceed 50 characters.");

        RuleFor(x => x.File)
            .NotNull().WithMessage("A file must be uploaded.")
            .Must(file => file.Length > 0).WithMessage("Uploaded file is empty.");
        
        RuleFor(x => x.File.ContentType)
            .Must(x => x == "image/jpeg" || x == "image/png" || x == "image/jpg")
            .WithMessage("Only JPEG and PNG images are allowed.");
    }
}
