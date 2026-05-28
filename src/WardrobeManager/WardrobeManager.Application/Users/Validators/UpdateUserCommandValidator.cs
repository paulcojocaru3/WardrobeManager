using FluentValidation;
using WardrobeManager.Application.Users.Commands;

namespace WardrobeManager.Application.Users.Validators;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        When(x => x.NewPassword != null, () =>
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Current password is required.");
        });

        When(x => x.Username != null, () =>
        {
            RuleFor(x => x.Username)
                .MinimumLength(3).WithMessage("Username must be at least 3 characters.");
        });

        When(x => x.Email != null, () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("A valid email address is required.");
        });

        When(x => x.NewPassword != null, () =>
        {
            RuleFor(x => x.NewPassword)
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
        });
    }
}
