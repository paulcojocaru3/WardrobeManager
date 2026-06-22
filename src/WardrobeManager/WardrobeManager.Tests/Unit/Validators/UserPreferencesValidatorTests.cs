using FluentValidation.TestHelper;
using WardrobeManager.Application.Users.Commands;
using WardrobeManager.Application.Users.Validators;

namespace WardrobeManager.Tests.Unit.Validators;

[Trait("Category", "Unit")]
public sealed class UserPreferencesValidatorTests
{
    private readonly UpdateUserPreferencesCommandValidator _sut = new();

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    public void RejectsDefaultReuseIntervalOutsideSupportedRange(int days)
    {
        var command = new UpdateUserPreferencesCommand(
            Guid.NewGuid(), null, null, null,
            DefaultReuseAfterDays: days,
            UpdateDefaultReuseAfterDays: true);

        _sut.TestValidate(command).ShouldHaveValidationErrorFor(x => x.DefaultReuseAfterDays!.Value);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(14)]
    public void AcceptsDefaultReuseIntervalBounds(int days)
    {
        var command = new UpdateUserPreferencesCommand(
            Guid.NewGuid(), null, null, null,
            DefaultReuseAfterDays: days,
            UpdateDefaultReuseAfterDays: true);

        _sut.TestValidate(command).ShouldNotHaveValidationErrorFor(x => x.DefaultReuseAfterDays!.Value);
    }

    [Fact]
    public void AcceptsNullToDisableReuse()
    {
        var command = new UpdateUserPreferencesCommand(
            Guid.NewGuid(), null, null, null,
            DefaultReuseAfterDays: null,
            UpdateDefaultReuseAfterDays: true);

        _sut.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }
}
