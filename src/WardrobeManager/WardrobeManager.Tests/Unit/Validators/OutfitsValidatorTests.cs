using FluentValidation.TestHelper;
using WardrobeManager.Application.Outfits.Commands;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Application.Outfits.Validators;

namespace WardrobeManager.Tests.Unit.Validators;

[Trait("Category", "Unit")]
public sealed class CreateOutfitCommandValidatorTests
{
    private readonly CreateOutfitCommandValidator _sut = new();
    private static CreateOutfitCommand Valid()
        => new(Guid.NewGuid(), "Weekend Look", new List<Guid> { Guid.NewGuid() });

    [Fact]
    public void Passes_ForValidCommand() => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenUserIdEmpty()
        => _sut.TestValidate(Valid() with { UserId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void Fails_WhenNameEmpty()
        => _sut.TestValidate(Valid() with { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Fails_WhenNameTooLong()
        => _sut.TestValidate(Valid() with { Name = new string('x', 101) }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Fails_WhenNoItems()
        => _sut.TestValidate(Valid() with { ItemIds = new List<Guid>() }).ShouldHaveValidationErrorFor(x => x.ItemIds);
}

[Trait("Category", "Unit")]
public sealed class UpdateOutfitCommandValidatorTests
{
    private readonly UpdateOutfitCommandValidator _sut = new();
    private static UpdateOutfitCommand Valid()
        => new(Guid.NewGuid(), "Updated Look", new List<Guid> { Guid.NewGuid() });

    [Fact]
    public void Passes_ForValidCommand() => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenIdEmpty()
        => _sut.TestValidate(Valid() with { Id = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void Fails_WhenNameEmpty()
        => _sut.TestValidate(Valid() with { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Fails_WhenNoItems()
        => _sut.TestValidate(Valid() with { ItemIds = new List<Guid>() }).ShouldHaveValidationErrorFor(x => x.ItemIds);
}

[Trait("Category", "Unit")]
public sealed class GetOutfitsQueryValidatorTests
{
    private readonly GetOutfitsQueryValidator _sut = new();

    [Fact]
    public void Passes_ForValidUserId()
        => _sut.TestValidate(new GetOutfitsQuery(Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenUserIdEmpty()
        => _sut.TestValidate(new GetOutfitsQuery(Guid.Empty)).ShouldHaveValidationErrorFor(x => x.UserId);
}

[Trait("Category", "Unit")]
public sealed class DeleteOutfitCommandValidatorTests
{
    private readonly DeleteOutfitCommandValidator _sut = new();

    [Fact]
    public void Passes_ForValidId()
        => _sut.TestValidate(new DeleteOutfitCommand(Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenIdEmpty()
        => _sut.TestValidate(new DeleteOutfitCommand(Guid.Empty)).ShouldHaveValidationErrorFor(x => x.Id);
}

[Trait("Category", "Unit")]
public sealed class GenerateOutfitCommandValidatorTests
{
    private readonly GenerateOutfitCommandValidator _sut = new();
    private static GenerateOutfitCommand Valid() => new(Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Passes_ForValidCommand() => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenUserIdEmpty()
        => _sut.TestValidate(Valid() with { UserId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void Fails_WhenStartItemEmpty()
        => _sut.TestValidate(Valid() with { StartItemId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.StartItemId);
}

[Trait("Category", "Unit")]
public sealed class GenerateAiOutfitCommandValidatorTests
{
    private readonly GenerateAiOutfitCommandValidator _sut = new();
    private static GenerateAiOutfitCommand Valid() => new(Guid.NewGuid(), Guid.NewGuid(), 0.5);

    [Fact]
    public void Passes_ForValidCommand() => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenUserIdEmpty()
        => _sut.TestValidate(Valid() with { UserId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void Fails_WhenStartItemEmpty()
        => _sut.TestValidate(Valid() with { StartItemId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.StartItemId);

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Fails_WhenThresholdOutOfRange(double threshold)
        => _sut.TestValidate(Valid() with { Threshold = threshold }).ShouldHaveValidationErrorFor(x => x.Threshold);
}

[Trait("Category", "Unit")]
public sealed class GenerateOutfitFromPromptCommandValidatorTests
{
    private readonly GenerateOutfitFromPromptCommandValidator _sut = new();
    private static GenerateOutfitFromPromptCommand Valid() => new(Guid.NewGuid(), "something smart for dinner");

    [Fact]
    public void Passes_ForValidCommand() => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenUserIdEmpty()
        => _sut.TestValidate(Valid() with { UserId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void Fails_WhenPromptEmpty()
        => _sut.TestValidate(Valid() with { Prompt = "" }).ShouldHaveValidationErrorFor(x => x.Prompt);

    [Fact]
    public void Fails_WhenPromptTooLong()
        => _sut.TestValidate(Valid() with { Prompt = new string('x', 1001) }).ShouldHaveValidationErrorFor(x => x.Prompt);
}
