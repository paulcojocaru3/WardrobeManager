using FluentValidation.TestHelper;
using WardrobeManager.Application.PlannedOutfits.Commands;
using WardrobeManager.Application.PlannedOutfits.Validators;

namespace WardrobeManager.Tests.Unit.Validators;

[Trait("Category", "Unit")]
public sealed class CreatePlannerEventCommandValidatorTests
{
    private readonly CreatePlannerEventCommandValidator _sut = new();
    private static CreatePlannerEventCommand Valid()
        => new(Guid.NewGuid(), "Wedding", "Formal", "Paris",
               new DateTime(2026, 6, 1), new DateTime(2026, 6, 2), new List<string>());

    [Fact]
    public void Passes_ForValidCommand() => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenUserIdEmpty()
        => _sut.TestValidate(Valid() with { UserId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void Fails_WhenNameEmpty()
        => _sut.TestValidate(Valid() with { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Fails_WhenTypeEmpty()
        => _sut.TestValidate(Valid() with { Type = "" }).ShouldHaveValidationErrorFor(x => x.Type);

    [Fact]
    public void Fails_WhenLocationEmpty()
        => _sut.TestValidate(Valid() with { Location = "" }).ShouldHaveValidationErrorFor(x => x.Location);

    [Fact]
    public void Fails_WhenEndDateBeforeStartDate()
        => _sut.TestValidate(Valid() with { StartDate = new DateTime(2026, 6, 2), EndDate = new DateTime(2026, 6, 1) })
              .ShouldHaveValidationErrorFor(x => x.EndDate.Date);
}

[Trait("Category", "Unit")]
public sealed class UpdatePlannerEventCommandValidatorTests
{
    private readonly UpdatePlannerEventCommandValidator _sut = new();
    private static UpdatePlannerEventCommand Valid()
        => new(Guid.NewGuid(), Guid.NewGuid(), "Wedding", "Formal", "Paris",
               new DateTime(2026, 6, 1), new DateTime(2026, 6, 2), new List<string>());

    [Fact]
    public void Passes_ForValidCommand() => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenUserIdEmpty()
        => _sut.TestValidate(Valid() with { UserId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void Fails_WhenPlannerEventIdEmpty()
        => _sut.TestValidate(Valid() with { PlannerEventId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.PlannerEventId);

    [Fact]
    public void Fails_WhenNameEmpty()
        => _sut.TestValidate(Valid() with { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Fails_WhenEndDateBeforeStartDate()
        => _sut.TestValidate(Valid() with { StartDate = new DateTime(2026, 6, 2), EndDate = new DateTime(2026, 6, 1) })
              .ShouldHaveValidationErrorFor(x => x.EndDate.Date);
}

[Trait("Category", "Unit")]
public sealed class ArchivePlannerEventCommandValidatorTests
{
    private readonly ArchivePlannerEventCommandValidator _sut = new();
    private static ArchivePlannerEventCommand Valid() => new(Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Passes_ForValidCommand() => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fails_WhenUserIdEmpty()
        => _sut.TestValidate(Valid() with { UserId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void Fails_WhenPlannerEventIdEmpty()
        => _sut.TestValidate(Valid() with { PlannerEventId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.PlannerEventId);
}
