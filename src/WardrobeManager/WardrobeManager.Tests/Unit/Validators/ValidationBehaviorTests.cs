using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using WardrobeManager.Application.Common.Behaviors;

namespace WardrobeManager.Tests.Unit.Validators;

[Trait("Category", "Unit")]
public sealed class ValidationBehaviorTests
{
    public sealed record DummyRequest(string Value);

    private static RequestHandlerDelegate<string> Next(string result = "ok")
        => () => Task.FromResult(result);

    [Fact]
    public async Task Handle_CallsNext_WhenNoValidators()
    {
        var sut = new ValidationBehavior<DummyRequest, string>(Array.Empty<IValidator<DummyRequest>>());

        var result = await sut.Handle(new DummyRequest("x"), Next(), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_CallsNext_WhenValidationPasses()
    {
        var validator = Substitute.For<IValidator<DummyRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<DummyRequest>>(), Arg.Any<CancellationToken>())
                 .Returns(new ValidationResult());
        var sut = new ValidationBehavior<DummyRequest, string>(new[] { validator });

        var result = await sut.Handle(new DummyRequest("x"), Next("passed"), CancellationToken.None);

        Assert.Equal("passed", result);
    }

    [Fact]
    public async Task Handle_Throws_WhenValidationFails()
    {
        var validator = Substitute.For<IValidator<DummyRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<DummyRequest>>(), Arg.Any<CancellationToken>())
                 .Returns(new ValidationResult(new[] { new ValidationFailure("Value", "required") }));
        var sut = new ValidationBehavior<DummyRequest, string>(new[] { validator });

        var nextCalled = false;
        RequestHandlerDelegate<string> next = () => { nextCalled = true; return Task.FromResult("ok"); };

        await Assert.ThrowsAsync<ValidationException>(() => sut.Handle(new DummyRequest("x"), next, CancellationToken.None));
        Assert.False(nextCalled);
    }
}
