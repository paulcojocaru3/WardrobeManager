using FluentValidation.TestHelper;
using WardrobeManager.Application.Clothing.Commands;
using WardrobeManager.Application.Clothing.Queries;
using WardrobeManager.Application.Clothing.Validators;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Validators;

[Trait("Category", "Unit")]
public sealed class AddClothingCommandValidatorTests
{
    private readonly AddClothingCommandValidator _sut = new();
    private static AddClothingCommand Valid()
        => new(Guid.NewGuid(), "Blue Shirt", ClothingType.Top, null, null, null, null, null, "base64data", null);

    [Fact]
    public void Passes_ForValidCommand() => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenUserIdEmpty()
        => _sut.TestValidate(Valid() with { UserId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.UserId);

    [Theory]
    [InlineData("")]
    public void Fails_WhenNameEmpty(string name)
        => _sut.TestValidate(Valid() with { Name = name }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Fails_WhenNameTooLong()
        => _sut.TestValidate(Valid() with { Name = new string('x', 101) }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Fails_WhenTypeNotInEnum()
        => _sut.TestValidate(Valid() with { Type = (ClothingType)999 }).ShouldHaveValidationErrorFor(x => x.Type);

    [Fact]
    public void Fails_WhenImageEmpty()
        => _sut.TestValidate(Valid() with { ProcessedImageB64 = "" }).ShouldHaveValidationErrorFor(x => x.ProcessedImageB64);
}

[Trait("Category", "Unit")]
public sealed class ProcessClothingCommandValidatorTests
{
    private readonly ProcessClothingCommandValidator _sut = new();
    private static ProcessClothingCommand Valid()
        => new(new byte[] { 1, 2, 3 }, "photo.png", "image/png", Guid.NewGuid(), "Shirt");

    [Fact]
    public void Passes_ForValidCommand() => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenUserIdEmpty()
        => _sut.TestValidate(Valid() with { UserId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void Fails_WhenNameEmpty()
        => _sut.TestValidate(Valid() with { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Fails_WhenFileContentEmpty()
        => _sut.TestValidate(Valid() with { FileContent = Array.Empty<byte>() }).ShouldHaveValidationErrorFor(x => x.FileContent);
}

[Trait("Category", "Unit")]
public sealed class DeleteClothingCommandValidatorTests
{
    private readonly DeleteClothingCommandValidator _sut = new();

    [Fact]
    public void Passes_ForValidId()
        => _sut.TestValidate(new DeleteClothingCommand(Guid.NewGuid(), Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenIdEmpty()
        => _sut.TestValidate(new DeleteClothingCommand(Guid.NewGuid(), Guid.Empty)).ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void Fails_WhenUserIdEmpty()
        => _sut.TestValidate(new DeleteClothingCommand(Guid.Empty, Guid.NewGuid())).ShouldHaveValidationErrorFor(x => x.UserId);
}

[Trait("Category", "Unit")]
public sealed class GetClothingItemsQueryValidatorTests
{
    private readonly GetClothingItemsQueryValidator _sut = new();

    [Fact]
    public void Passes_ForValidUserId()
        => _sut.TestValidate(new GetClothingItemsQuery(Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenUserIdEmpty()
        => _sut.TestValidate(new GetClothingItemsQuery(Guid.Empty)).ShouldHaveValidationErrorFor(x => x.UserId);
}

[Trait("Category", "Unit")]
public sealed class UpdateClothingCommandValidatorTests
{
    private readonly UpdateClothingCommandValidator _sut = new();
    private static UpdateClothingCommand Valid()
        => new(Guid.NewGuid(), Guid.NewGuid(), "Blue Shirt", ClothingType.Top, "blue", "Men", "Summer", "Casual", "tshirts");

    [Fact]
    public void Passes_ForValidCommand() => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenIdEmpty()
        => _sut.TestValidate(Valid() with { Id = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void Fails_WhenUserIdEmpty()
        => _sut.TestValidate(Valid() with { UserId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void Fails_WhenNameEmpty()
        => _sut.TestValidate(Valid() with { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Fails_WhenColorTooLong()
        => _sut.TestValidate(Valid() with { Color = new string('x', 41) }).ShouldHaveValidationErrorFor(x => x.Color);

    [Fact]
    public void Fails_WhenSubTypeTooLong()
        => _sut.TestValidate(Valid() with { SubType = new string('x', 81) }).ShouldHaveValidationErrorFor(x => x.SubType);
}

[Trait("Category", "Unit")]
public sealed class FindSimilarItemsQueryValidatorTests
{
    private readonly FindSimilarItemsQueryValidator _sut = new();
    private static FindSimilarItemsQuery Valid() => new(Guid.NewGuid(), Guid.NewGuid(), 10);

    [Fact]
    public void Passes_ForValidQuery() => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenItemIdEmpty()
        => _sut.TestValidate(Valid() with { ItemId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.ItemId);

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Fails_WhenLimitOutOfRange(int limit)
        => _sut.TestValidate(Valid() with { Limit = limit }).ShouldHaveValidationErrorFor(x => x.Limit);
}
