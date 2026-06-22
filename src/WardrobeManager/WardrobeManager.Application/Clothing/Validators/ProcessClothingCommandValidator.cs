using FluentValidation;
using WardrobeManager.Application.Clothing.Commands;

namespace WardrobeManager.Application.Clothing.Validators;

public sealed class ProcessClothingCommandValidator : AbstractValidator<ProcessClothingCommand>
{
    private static readonly string[] AllowedImageContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const int MaxUploadBytes = 10 * 1024 * 1024;

    public ProcessClothingCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType)
            .Must(ct => AllowedImageContentTypes.Contains(ct, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Only JPEG, PNG, and WebP images are supported.");
        RuleFor(x => x.FileContent)
            .NotEmpty()
            .Must(bytes => bytes.Length <= MaxUploadBytes)
            .WithMessage("Image file is too large.");
    }
}
