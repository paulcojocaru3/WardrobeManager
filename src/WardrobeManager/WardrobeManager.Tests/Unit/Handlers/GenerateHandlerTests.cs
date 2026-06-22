using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Commands;
using WardrobeManager.Application.Outfits.Feasibility;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public sealed class GenerateAiOutfitCommandHandlerTests
{
    private readonly IOutfitGenerator _generator = Substitute.For<IOutfitGenerator>();
    private readonly IWeatherService _weather = Substitute.For<IWeatherService>();
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IOutfitFeedbackRepository _feedback = Substitute.For<IOutfitFeedbackRepository>();
    private readonly IOccasionFormalityRules _occasion = Substitute.For<IOccasionFormalityRules>();
    private readonly IOutfitStylist _stylist = Substitute.For<IOutfitStylist>();
    private readonly IItemPairScoreRepository _pairScores = Substitute.For<IItemPairScoreRepository>();
    private readonly IUserLearningProfileRepository _learningProfiles = Substitute.For<IUserLearningProfileRepository>();
    private readonly IMlService _ml = Substitute.For<IMlService>();
    private readonly IThermalRules _thermal = Substitute.For<IThermalRules>();

    private GenerateAiOutfitCommandHandler Sut() => new(
        _generator,
        _weather,
        _clothing,
        _users,
        _feedback,
        _occasion,
        new StylistOutfitComposer(_stylist, _pairScores, _learningProfiles, NullLogger<StylistOutfitComposer>.Instance),
        new StylistCandidatePoolBuilder(_clothing, _ml, _thermal, NullLogger<StylistCandidatePoolBuilder>.Instance),
        new StylistSettings(),
        NullLogger<GenerateAiOutfitCommandHandler>.Instance);

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
    public async Task Handle_AnchorsOnLeastWornItem_AndPrefersUnused_InRediscover()
    {
        var seed = new ClothingItem { Id = Guid.NewGuid() };
        _clothing.GetLeastWornCandidatesAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ClothingItem> { seed });
        _feedback.GetRecentlyShownItemIdsAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<ClothingType?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());
        _generator.GenerateAiOutfitAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<OutfitGenerationOptions>(), Arg.Any<CancellationToken>())
            .Returns(new AiGeneratedOutfitDto());

        await Sut().Handle(new GenerateAiOutfitCommand(Guid.NewGuid(), StartItemId: null, AnchorOnUnused: true), CancellationToken.None);

        await _generator.Received(1).GenerateAiOutfitAsync(
            Arg.Any<Guid>(), seed.Id,
            Arg.Is<OutfitGenerationOptions>(o => o.PreferUnusedItems),
            Arg.Any<CancellationToken>());
    }
}

[Trait("Category", "Unit")]
public sealed class GenerateOutfitCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private readonly IOutfitRepository _outfits = Substitute.For<IOutfitRepository>();
    private readonly IOutfitGenerator _generator = Substitute.For<IOutfitGenerator>();

    private GenerateOutfitCommandHandler Sut() => new(_users, _clothing, _outfits, _generator);

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
