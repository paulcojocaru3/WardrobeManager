using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Explaining;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;
using WardrobeManager.Infrastructure.ExternalServices;

namespace WardrobeManager.Tests.Unit.Explaining;

[Trait("Category", "Unit")]
public sealed class StylingNotesTests
{
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private readonly IWeatherService _weather = Substitute.For<IWeatherService>();
    private readonly IStylingNotesService _notes = Substitute.For<IStylingNotesService>();
    private readonly Guid _userId = Guid.NewGuid();

    private ExplainOutfitQueryHandler Sut() => new(
        _clothing, _weather, Array.Empty<IOutfitEvaluator>(), _notes, NullLogger<ExplainOutfitQueryHandler>.Instance);

    [Fact]
    public async Task Handle_GroundsFactsFromSelectedItems_AndReturnsNotes()
    {
        var top = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top, Name = "white tee", Color = "white", SubType = "tshirts", Usage = "Casual" };
        var bottom = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Name = "charcoal jeans", Color = "charcoal", SubType = "jeans", Usage = "Casual" };
        _clothing.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ClothingItem> { top, bottom });

        OutfitExplanation? captured = null;
        _notes.GenerateAsync(Arg.Do<OutfitExplanation>(e => captured = e), Arg.Any<CancellationToken>())
            .Returns(new[] { "A relaxed casual look." });

        var result = await Sut().Handle(
            new ExplainOutfitQuery(_userId, new[] { top.Id, bottom.Id }, Style: "Casual"), CancellationToken.None);

        Assert.Single(result.Notes);
        Assert.NotNull(captured);
        Assert.Equal("Casual", captured!.Style);
        Assert.Equal(2, captured.Pieces.Count);
        Assert.Contains(captured.Pieces, p => p.Slot == "top" && p.Color == "white");
        Assert.Contains(captured.Pieces, p => p.Slot == "bottoms" && p.SubType == "jeans");
    }

    [Fact]
    public async Task Handle_ReturnsEmpty_WhenNoItems()
    {
        _clothing.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ClothingItem>());

        var result = await Sut().Handle(new ExplainOutfitQuery(_userId, new[] { Guid.NewGuid() }), CancellationToken.None);

        Assert.Empty(result.Notes);
    }

    [Fact]
    public async Task TemplateService_GeneratesNeutralBaseAccentNotes()
    {
        var sut = new TemplateStylingNotesService();

        var explanation = new OutfitExplanation
        {
            Style = "Casual",
            Pieces = new List<OutfitPieceFact>
            {
                new() { Slot = "top", Name = "white tee", Color = "white" },
                new() { Slot = "bottoms", Name = "charcoal jeans", Color = "charcoal" },
                new() { Slot = "shoes", Name = "red sneakers", Color = "red" },
            },
        };

        var notes = await sut.GenerateAsync(explanation);

        Assert.NotEmpty(notes);
        Assert.Contains(notes, n => n.Contains("neutral base plus accent", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(notes, n => n.Contains("red", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TemplateService_GeneratesInsightWithWeatherAndNoTips()
    {
        var sut = new TemplateStylingNotesService();

        var topId = Guid.NewGuid();
        var explanation = new OutfitExplanation
        {
            Style = "Casual",
            Weather = new WeatherData(6, "Rain", "Winter", FeelsLike: 3, RainChance: 75, ConditionDetail: "light rain"),
            Pieces = new List<OutfitPieceFact>
            {
                new() { ItemId = topId, Slot = "top", Name = "white tee", Color = "white", Material = "cotton", Description = "white cotton tshirts" },
                new() { ItemId = Guid.NewGuid(), Slot = "bottoms", Name = "charcoal jeans", Color = "charcoal", Description = "charcoal jeans" },
                new() { ItemId = Guid.NewGuid(), Slot = "shoes", Name = "black boots", Color = "black", Description = "black leather boots" },
            },
        };

        var insight = await sut.GenerateInsightAsync(explanation);

        Assert.False(string.IsNullOrWhiteSpace(insight.Headline));
        Assert.Equal(3, insight.Items.Count);
        Assert.Contains(insight.Items, i => i.ItemId == topId);
        Assert.NotNull(insight.WeatherAdvice);
        Assert.Contains("75%", insight.WeatherAdvice!);
        Assert.DoesNotContain("Weather:", insight.WeatherAdvice!);
        Assert.Empty(insight.Tips);
    }
}
