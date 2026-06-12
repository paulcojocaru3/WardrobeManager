using FluentValidation.TestHelper;
using WardrobeManager.Application.Users.Commands;
using WardrobeManager.Application.Users.Queries;
using WardrobeManager.Application.Users.Validators;

namespace WardrobeManager.Tests.Unit.Validators;

[Trait("Category", "Unit")]
public sealed class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _sut = new();
    private static RegisterUserCommand Valid() => new("alice@example.com", "passw0rd", "alice");

    [Fact]
    public void Passes_ForValidCommand() => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("ab")] // too short
    public void Fails_ForBadUsername(string username)
        => _sut.TestValidate(Valid() with { Username = username }).ShouldHaveValidationErrorFor(x => x.Username);

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Fails_ForBadEmail(string email)
        => _sut.TestValidate(Valid() with { Email = email }).ShouldHaveValidationErrorFor(x => x.Email);

    [Theory]
    [InlineData("")]
    [InlineData("short1")]    // < 8
    [InlineData("password")]  // no digit
    [InlineData("12345678")]  // no letter
    public void Fails_ForBadPassword(string password)
        => _sut.TestValidate(Valid() with { Password = password }).ShouldHaveValidationErrorFor(x => x.Password);
}

[Trait("Category", "Unit")]
public sealed class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _sut = new();
    private static UpdateUserCommand Valid()
        => new(Guid.NewGuid(), Username: "alice", Email: "alice@example.com", NewPassword: null, CurrentPassword: "current");

    [Fact]
    public void Passes_ForValidCommand() => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenUserIdEmpty()
        => _sut.TestValidate(Valid() with { UserId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void Fails_WhenChangingPasswordWithoutCurrent()
        => _sut.TestValidate(Valid() with { NewPassword = "passw0rd", CurrentPassword = "" })
              .ShouldHaveValidationErrorFor(x => x.CurrentPassword);

    [Fact]
    public void Fails_ForShortUsername_WhenProvided()
        => _sut.TestValidate(Valid() with { Username = "ab" }).ShouldHaveValidationErrorFor(x => x.Username);

    [Fact]
    public void Fails_ForInvalidEmail_WhenProvided()
        => _sut.TestValidate(Valid() with { Email = "bad" }).ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void Fails_ForWeakNewPassword()
        => _sut.TestValidate(Valid() with { NewPassword = "short1", CurrentPassword = "current" })
              .ShouldHaveValidationErrorFor(x => x.NewPassword);
}

[Trait("Category", "Unit")]
public sealed class LoginUserQueryValidatorTests
{
    private readonly LoginUserQueryValidator _sut = new();
    private static LoginUserQuery Valid() => new("alice@example.com", "secret");

    [Fact]
    public void Passes_ForValidQuery() => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("nope")]
    public void Fails_ForBadEmail(string email)
        => _sut.TestValidate(Valid() with { Email = email }).ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void Fails_WhenPasswordEmpty()
        => _sut.TestValidate(Valid() with { Password = "" }).ShouldHaveValidationErrorFor(x => x.Password);
}
