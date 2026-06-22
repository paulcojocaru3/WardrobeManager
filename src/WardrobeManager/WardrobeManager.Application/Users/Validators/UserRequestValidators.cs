using FluentValidation;
using WardrobeManager.Application.Users.Commands;

namespace WardrobeManager.Application.Users.Validators;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class UpdateUserPreferencesCommandValidator : AbstractValidator<UpdateUserPreferencesCommand>
{
    private static readonly string[] AllowedVarietyLevels = ["low", "normal", "high"];

    public UpdateUserPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleForEach(x => x.FavoriteColors).MaximumLength(40);
        RuleForEach(x => x.AvoidColors).MaximumLength(40);
        RuleFor(x => x.PreferredCity).MaximumLength(120);
        RuleFor(x => x.ThemePreference).MaximumLength(40);
        RuleFor(x => x.OuterwearMode).MaximumLength(40);

        When(x => x.OuterwearTempThreshold.HasValue, () =>
        {
            RuleFor(x => x.OuterwearTempThreshold!.Value).InclusiveBetween(-30, 50);
        });

        When(x => x.VarietyLevel != null, () =>
        {
            RuleFor(x => x.VarietyLevel)
                .Must(value => AllowedVarietyLevels.Contains(value, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Variety level is not supported.");
        });

        When(x => x.UpdateDefaultReuseAfterDays && x.DefaultReuseAfterDays.HasValue, () =>
        {
            RuleFor(x => x.DefaultReuseAfterDays!.Value)
                .InclusiveBetween(2, 14)
                .WithMessage("Default reuse interval must be between 2 and 14 days.");
        });
    }
}
