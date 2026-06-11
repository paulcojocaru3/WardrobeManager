using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Commands;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Application.Outfits.Prompting;
using WardrobeManager.Application.Outfits.Validators;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public sealed class GenerateAiOutfitCommandHandlerTests
{
    private readonly IOutfitGenerator _generator = Substitute.For<IOutfitGenerator>();
    private readonly IWeatherService _weather = Substitute.For<IWeatherService>();

    private GenerateAiOutfitCommandHandler Sut() => new(_generator, _weather, new GenerateAiOutfitCommandValidator());

    [Fact]
    public async Task Handle_ResolvesWeather_WhenCityGiven()
    {
        var dto = new AiGeneratedOutfitDto { Name = "Look" };
        _weather.GetCurrentWeatherAsync("Paris", Arg.Any<CancellationToken>()).Returns(new WeatherData(18, "Clear", "Spring"));
        _generator.GenerateAiOutfitAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<OutfitGenerationOptions>(), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await Sut().Handle(new GenerateAiOutfitCommand(Guid.NewGuid(), Guid.NewGuid(), 0.5, "Paris"), CancellationToken.None);

        Assert.Same(dto, result);
        await _weather.Received(1).GetCurrentWeatherAsync("Paris", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SkipsWeather_WhenNoCity()
    {
        _generator.GenerateAiOutfitAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<OutfitGenerationOptions>(), Arg.Any<CancellationToken>())
            .Returns(new AiGeneratedOutfitDto());

        await Sut().Handle(new GenerateAiOutfitCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await _weather.DidNotReceive().GetCurrentWeatherAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_ForInvalidThreshold()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => Sut().Handle(new GenerateAiOutfitCommand(Guid.NewGuid(), Guid.NewGuid(), 5.0), CancellationToken.None));
    }
}

[Trait("Category", "Unit")]
public sealed class GenerateOutfitCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private readonly IOutfitRepository _outfits = Substitute.For<IOutfitRepository>();
    private readonly IOutfitGenerator _generator = Substitute.For<IOutfitGenerator>();

    private GenerateOutfitCommandHandler Sut() => new(_users, _clothing, _outfits, _generator, new GenerateOutfitCommandValidator());

    [Fact]
    public async Task Handle_PersistsGeneratedOutfit()
    {
        var userId = Guid.NewGuid();
        _users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new User { Id = userId });
        var item = new ClothingItem { Id = Guid.NewGuid(), Name = "Top", Type = ClothingType.Top };
        var dto = new AiGeneratedOutfitDto { Name = "Generated", SelectedItems = new List<SimilarItemDto> { new() { Id = item.Id } } };
        _generator.GenerateAiOutfitAsync(userId, Arg.Any<Guid>(), Arg.Any<OutfitGenerationOptions>(), Arg.Any<CancellationToken>()).Returns(dto);
        _clothing.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<ClothingItem> { item });

        var result = await Sut().Handle(new GenerateOutfitCommand(userId, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal("Generated", result.Name);
        Assert.Single(result.Items);
        await _outfits.Received(1).AddAsync(Arg.Is<Outfit>(o => o.IsAiGenerated), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenUserNotFound()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Sut().Handle(new GenerateOutfitCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }
}

[Trait("Category", "Unit")]
public sealed class GenerateOutfitFromPromptCommandHandlerTests
{
    private readonly IPromptIntentService _intent = Substitute.For<IPromptIntentService>();
    private readonly IOccasionClassifier _occasion = Substitute.For<IOccasionClassifier>();
    private readonly IGarmentClassifier _garment = Substitute.For<IGarmentClassifier>();
    private readonly IStartItemSelector _selector = Substitute.For<IStartItemSelector>();
    private readonly IOutfitGenerator _generator = Substitute.For<IOutfitGenerator>();
    private readonly IWeatherService _weather = Substitute.For<IWeatherService>();

    private GenerateOutfitFromPromptCommandHandler Sut() => new(
        _intent, _occasion, _garment, _selector, _generator, _weather,
        new GenerateOutfitFromPromptCommandValidator(),
        NullLogger<GenerateOutfitFromPromptCommandHandler>.Instance);

    private void GivenParsed(PromptIntent intent)
        => _intent.ParseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(intent);

    [Fact]
    public async Task Handle_EnrichesIntent_AndGeneratesOutfit()
    {
        GivenParsed(new PromptIntent { City = "Paris" });
        _occasion.ClassifyStyle(Arg.Any<string>()).Returns("Casual");
        _garment.Detect(Arg.Any<string>()).Returns(new[] { new RequestedGarment("shorts", ClothingType.Bottom) });
        _weather.GetCurrentWeatherAsync("Paris", Arg.Any<CancellationToken>()).Returns(new WeatherData(28, "Clear", "Summer"));
        var seed = new ClothingItem { Id = Guid.NewGuid() };
        _selector.SelectAsync(Arg.Any<Guid>(), Arg.Any<PromptIntent>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<WeatherData?>(), Arg.Any<CancellationToken>()).Returns(seed);
        var dto = new AiGeneratedOutfitDto { Name = "Prompt Look" };
        _generator.GenerateAiOutfitAsync(Arg.Any<Guid>(), seed.Id, Arg.Any<OutfitGenerationOptions>(), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await Sut().Handle(new GenerateOutfitFromPromptCommand(Guid.NewGuid(), "shorts for Paris"), CancellationToken.None);

        Assert.Same(dto, result.Outfit);
        Assert.Equal("Casual", result.Intent.Style);          // occasion map overrode
        Assert.Single(result.Intent.RequestedGarments);       // garment detected
    }

    [Fact]
    public async Task Handle_ContinuesWithoutWeather_WhenWeatherFails()
    {
        GivenParsed(new PromptIntent { City = "Nowhere" });
        _occasion.ClassifyStyle(Arg.Any<string>()).Returns((string?)null);
        _garment.Detect(Arg.Any<string>()).Returns(Array.Empty<RequestedGarment>());
        _weather.GetCurrentWeatherAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<WeatherData>(_ => throw new InvalidOperationException("no key"));
        _selector.SelectAsync(Arg.Any<Guid>(), Arg.Any<PromptIntent>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<WeatherData?>(), Arg.Any<CancellationToken>())
            .Returns(new ClothingItem { Id = Guid.NewGuid() });
        _generator.GenerateAiOutfitAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<OutfitGenerationOptions>(), Arg.Any<CancellationToken>())
            .Returns(new AiGeneratedOutfitDto());

        var result = await Sut().Handle(new GenerateOutfitFromPromptCommand(Guid.NewGuid(), "something"), CancellationToken.None);

        Assert.NotNull(result.Outfit);
    }

    [Fact]
    public async Task Handle_Throws_WhenNoSeedItem()
    {
        GivenParsed(new PromptIntent());
        _occasion.ClassifyStyle(Arg.Any<string>()).Returns((string?)null);
        _garment.Detect(Arg.Any<string>()).Returns(Array.Empty<RequestedGarment>());
        _selector.SelectAsync(Arg.Any<Guid>(), Arg.Any<PromptIntent>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<WeatherData?>(), Arg.Any<CancellationToken>())
            .Returns((ClothingItem?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Sut().Handle(new GenerateOutfitFromPromptCommand(Guid.NewGuid(), "anything"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_GenericGarmentWord_DoesNotConstrainSubType()
    {
        var captured = await CaptureOptionsForGarment(new RequestedGarment("pants", ClothingType.Bottom));

        Assert.True(captured.GarmentConstraints.TryGetValue(ClothingType.Bottom, out var spec));
        Assert.Null(spec!.SubType); // "pants" is generic -> type only, no sub-type hard filter
    }

    [Fact]
    public async Task Handle_SpecificGarmentWord_ConstrainsSubType()
    {
        var captured = await CaptureOptionsForGarment(new RequestedGarment("shorts", ClothingType.Bottom));

        Assert.True(captured.GarmentConstraints.TryGetValue(ClothingType.Bottom, out var spec));
        Assert.Equal("shorts", spec!.SubType); // a specific sub-type is still enforced
    }

    private async Task<OutfitGenerationOptions> CaptureOptionsForGarment(RequestedGarment garment)
    {
        GivenParsed(new PromptIntent());
        _occasion.ClassifyStyle(Arg.Any<string>()).Returns((string?)null);
        _garment.Detect(Arg.Any<string>()).Returns(new[] { garment });
        _selector.SelectAsync(Arg.Any<Guid>(), Arg.Any<PromptIntent>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<WeatherData?>(), Arg.Any<CancellationToken>())
            .Returns(new ClothingItem { Id = Guid.NewGuid() });

        OutfitGenerationOptions? captured = null;
        _generator.GenerateAiOutfitAsync(Arg.Any<Guid>(), Arg.Any<Guid>(),
                Arg.Do<OutfitGenerationOptions>(o => captured = o), Arg.Any<CancellationToken>())
            .Returns(new AiGeneratedOutfitDto());

        await Sut().Handle(new GenerateOutfitFromPromptCommand(Guid.NewGuid(), "prompt"), CancellationToken.None);

        Assert.NotNull(captured);
        return captured!;
    }
}
