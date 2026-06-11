using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Generation;

[Trait("Category", "Unit")]
public sealed class OutfitGeneratorTests
{
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IOutfitFeedbackRepository _feedback = Substitute.For<IOutfitFeedbackRepository>();
    private readonly IUserEvaluatorWeightsRepository _weights = Substitute.For<IUserEvaluatorWeightsRepository>();
    private readonly Guid _userId = Guid.NewGuid();

    private static readonly IOutfitEvaluator[] Evaluators =
    {
        new WeatherEvaluator(), new StyleEvaluator(), new ColorHarmonyEvaluator(),
        new ColorPreferenceEvaluator(), new VarietyEvaluator(),
    };

    private OutfitGenerator Sut() => new(_clothing, _users, _feedback, _weights, Evaluators, NullLogger<OutfitGenerator>.Instance);

    private ClothingItem Start()
    {
        var start = new ClothingItem
        {
            Id = Guid.NewGuid(), UserId = _userId, Type = ClothingType.Top,
            Name = "Seed", ProcessedImageUrl = "seed", Embedding = new[] { 1f, 0f, 0f },
        };
        _clothing.GetByIdAsync(start.Id, Arg.Any<CancellationToken>()).Returns(start);
        _clothing.GetWearRecencyAsync(_userId, Arg.Any<CancellationToken>()).Returns(new Dictionary<Guid, DateTime>());
        _weights.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((UserEvaluatorWeights?)null);
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new User { Id = _userId });
        return start;
    }

    private void GivenCandidates(params (ClothingItem item, double sim)[] candidates)
        => _clothing.GetSimilarItemsAsync(Arg.Any<Guid>(), Arg.Any<float[]>(), Arg.Any<ClothingType?>(),
                Arg.Any<int>(), Arg.Any<double?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(candidates.Select(c => (c.item, c.sim)).ToList());

    private ClothingItem CasualTopSeed()
    {
        var start = new ClothingItem
        {
            Id = Guid.NewGuid(), UserId = _userId, Type = ClothingType.Top,
            Name = "Tee", ProcessedImageUrl = "s", Usage = "Casual", Embedding = new[] { 1f, 0f, 0f },
        };
        _clothing.GetByIdAsync(start.Id, Arg.Any<CancellationToken>()).Returns(start);
        _clothing.GetWearRecencyAsync(_userId, Arg.Any<CancellationToken>()).Returns(new Dictionary<Guid, DateTime>());
        _weights.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((UserEvaluatorWeights?)null);
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new User { Id = _userId });
        return start;
    }

    [Fact]
    public async Task GenerateAiOutfit_BuildsOutfit_AndLogsImpressions()
    {
        var start = Start();
        var c1 = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Name = "c1", ProcessedImageUrl = "c1", Embedding = new[] { 0f, 1f, 0f } };
        var c2 = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Name = "c2", ProcessedImageUrl = "c2", Embedding = new[] { 0f, 1f, 0f } };
        GivenCandidates((c1, 0.8), (c2, 0.7));

        var result = await Sut().GenerateAiOutfitAsync(_userId, start.Id, new OutfitGenerationOptions { Threshold = 0.5 });

        Assert.NotEqual(Guid.Empty, result.GenerationId);
        Assert.True(result.SelectedItems.Count >= 2);   // seed + slot bests
        Assert.NotEmpty(result.RecommendationsPerType);
        Assert.True(result.IsValid);
        await _feedback.Received().AddImpressionsAsync(Arg.Any<IEnumerable<OutfitFeedback>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAiOutfit_AppliesLearnedWeights_AndStyle()
    {
        var start = Start();
        _weights.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new UserEvaluatorWeights
        {
            UserId = _userId, MlWeight = 0.3, Weights = new() { ["Style"] = 0.5, ["Weather"] = 0.4 },
        });
        var c1 = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Name = "jeans", ProcessedImageUrl = "c1", SubType = "jeans", Usage = "Casual", Color = "blue", Season = "All Seasons", Embedding = new[] { 0f, 1f, 0f } };
        GivenCandidates((c1, 0.9));

        var options = new OutfitGenerationOptions
        {
            Threshold = 0.4, Style = "Casual", Weather = new WeatherData(20, "Clear", "Spring"),
            GarmentConstraints = new Dictionary<ClothingType, GarmentSpec>
            {
                [ClothingType.Bottom] = new GarmentSpec { Type = ClothingType.Bottom, SubType = "jeans" },
            },
        };

        var result = await Sut().GenerateAiOutfitAsync(_userId, start.Id, options);

        Assert.Contains("Casual", result.Name);
        Assert.True(result.SelectedItems.Count >= 2);
    }

    [Fact]
    public async Task GenerateAiOutfit_FollowsSeedStyle_WhenNoExplicitStyle()
    {
        // Casual tee seed, no explicit style. The casual shoes should win the slot over the formal
        // ones even though the formal pair has a higher raw similarity — the seed's style steers it.
        var start = new ClothingItem
        {
            Id = Guid.NewGuid(), UserId = _userId, Type = ClothingType.Top,
            Name = "Tee", ProcessedImageUrl = "s", Usage = "Casual", Embedding = new[] { 1f, 0f, 0f },
        };
        _clothing.GetByIdAsync(start.Id, Arg.Any<CancellationToken>()).Returns(start);
        _clothing.GetWearRecencyAsync(_userId, Arg.Any<CancellationToken>()).Returns(new Dictionary<Guid, DateTime>());
        _weights.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((UserEvaluatorWeights?)null);
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(new User { Id = _userId });

        var casualShoes = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Shoes, Name = "sneakers", ProcessedImageUrl = "c", Usage = "Casual", Embedding = new[] { 0f, 1f, 0f } };
        var formalShoes = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Shoes, Name = "oxfords", ProcessedImageUrl = "f", Usage = "Formal", Embedding = new[] { 0f, 1f, 0f } };
        GivenCandidates((formalShoes, 0.85), (casualShoes, 0.8)); // formal ranks higher by similarity

        var result = await Sut().GenerateAiOutfitAsync(_userId, start.Id, new OutfitGenerationOptions { Threshold = 0.4 });

        var shoesSlot = result.RecommendationsPerType.First(r => r.Type == ClothingType.Shoes);
        Assert.Equal(casualShoes.Id, shoesSlot.TopCandidates[0].Id);
    }

    [Fact]
    public async Task GenerateAiOutfit_IncludesSeedSlotRecommendation_WithSeedAndAlternatives()
    {
        var start = CasualTopSeed();
        var altTop1 = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top, Name = "alt1", ProcessedImageUrl = "a1", Usage = "Casual", Embedding = new[] { 1f, 0f, 0f } };
        var altTop2 = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top, Name = "alt2", ProcessedImageUrl = "a2", Usage = "Casual", Embedding = new[] { 1f, 0f, 0f } };
        GivenCandidates((altTop1, 0.9), (altTop2, 0.85)); // seed not in pool -> prepended

        var result = await Sut().GenerateAiOutfitAsync(_userId, start.Id, new OutfitGenerationOptions { Threshold = 0.4 });

        var seedRec = result.RecommendationsPerType.FirstOrDefault(r => r.Type == ClothingType.Top);
        Assert.NotNull(seedRec);
        Assert.Contains(seedRec!.TopCandidates, c => c.Id == start.Id);                 // seed is in its own slot
        Assert.Contains(seedRec.TopCandidates, c => c.Id == altTop1.Id || c.Id == altTop2.Id); // + alternatives
    }

    [Fact]
    public async Task GenerateAiOutfit_SeedSlotAlternatives_AreScoredByStyle()
    {
        var start = CasualTopSeed();
        var casualTop = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top, Name = "casual", ProcessedImageUrl = "c", Usage = "Casual", Embedding = new[] { 1f, 0f, 0f } };
        var formalTop = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top, Name = "formal", ProcessedImageUrl = "f", Usage = "Formal", Embedding = new[] { 1f, 0f, 0f } };
        GivenCandidates((formalTop, 0.9), (casualTop, 0.85)); // formal has higher raw similarity

        var result = await Sut().GenerateAiOutfitAsync(_userId, start.Id, new OutfitGenerationOptions { Threshold = 0.4 });

        var seedRec = result.RecommendationsPerType.First(r => r.Type == ClothingType.Top);
        var alts = seedRec.TopCandidates.Where(c => c.Id != start.Id).Select(c => c.Id).ToList();
        Assert.True(alts.IndexOf(casualTop.Id) < alts.IndexOf(formalTop.Id)); // casual ranks above formal
    }

    [Fact]
    public async Task GenerateAiOutfit_PerSlotDesiredColor_KeepsOnlyThatColor_WhenAvailable()
    {
        var start = Start();
        var blackBottom = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Name = "black", ProcessedImageUrl = "b", Color = "black", Embedding = new[] { 0f, 1f, 0f } };
        var blueBottom = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Name = "blue", ProcessedImageUrl = "u", Color = "blue", Embedding = new[] { 0f, 1f, 0f } };
        GivenCandidates((blueBottom, 0.95), (blackBottom, 0.8)); // blue ranks higher, but black is required

        var options = new OutfitGenerationOptions
        {
            Threshold = 0.4,
            GarmentConstraints = new Dictionary<ClothingType, GarmentSpec>
            {
                [ClothingType.Bottom] = new GarmentSpec { Type = ClothingType.Bottom, DesiredColors = new[] { "black" } },
            },
        };

        var result = await Sut().GenerateAiOutfitAsync(_userId, start.Id, options);

        var bottom = result.RecommendationsPerType.First(r => r.Type == ClothingType.Bottom);
        Assert.All(bottom.TopCandidates, c => Assert.NotEqual(blueBottom.Id, c.Id)); // blue filtered out
        Assert.Contains(bottom.TopCandidates, c => c.Id == blackBottom.Id);          // black guaranteed
    }

    [Fact]
    public async Task GenerateAiOutfit_PerSlotAvoidColors_DropsThoseColors()
    {
        var start = Start();
        var black = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Name = "black", ProcessedImageUrl = "b", Color = "black", Embedding = new[] { 0f, 1f, 0f } };
        var white = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Name = "white", ProcessedImageUrl = "w", Color = "white", Embedding = new[] { 0f, 1f, 0f } };
        var green = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Name = "green", ProcessedImageUrl = "g", Color = "green", Embedding = new[] { 0f, 1f, 0f } };
        GivenCandidates((black, 0.95), (white, 0.9), (green, 0.7)); // black/white rank higher but are avoided

        var options = new OutfitGenerationOptions
        {
            Threshold = 0.4,
            GarmentConstraints = new Dictionary<ClothingType, GarmentSpec>
            {
                [ClothingType.Bottom] = new GarmentSpec { Type = ClothingType.Bottom, AvoidColors = new[] { "black", "white" } },
            },
        };

        var result = await Sut().GenerateAiOutfitAsync(_userId, start.Id, options);

        var bottom = result.RecommendationsPerType.First(r => r.Type == ClothingType.Bottom);
        Assert.All(bottom.TopCandidates, c => Assert.Equal(green.Id, c.Id)); // only the non-avoided color survives
    }

    [Fact]
    public async Task GenerateAiOutfit_Warns_WhenAvoidColorCannotBeHonored()
    {
        var start = Start();
        // Only black bottoms exist, but the user asked to avoid black for the bottom -> kept + warned.
        var blackBottom = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Name = "b", ProcessedImageUrl = "b", Color = "black", Embedding = new[] { 0f, 1f, 0f } };
        GivenCandidates((blackBottom, 0.9));

        var options = new OutfitGenerationOptions
        {
            Threshold = 0.4,
            GarmentConstraints = new Dictionary<ClothingType, GarmentSpec>
            {
                [ClothingType.Bottom] = new GarmentSpec { Type = ClothingType.Bottom, AvoidColors = new[] { "black" } },
            },
        };

        var result = await Sut().GenerateAiOutfitAsync(_userId, start.Id, options);

        Assert.Contains(result.Warnings, w => w.Contains("avoid", StringComparison.OrdinalIgnoreCase) && w.Contains("black"));
    }

    [Fact]
    public async Task GenerateAiOutfit_Warns_WhenDesiredColorUnavailable()
    {
        var start = Start();
        var blackBottom = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Name = "b", ProcessedImageUrl = "b", Color = "black", Embedding = new[] { 0f, 1f, 0f } };
        GivenCandidates((blackBottom, 0.9)); // no blue bottom available

        var options = new OutfitGenerationOptions
        {
            Threshold = 0.4,
            GarmentConstraints = new Dictionary<ClothingType, GarmentSpec>
            {
                [ClothingType.Bottom] = new GarmentSpec { Type = ClothingType.Bottom, DesiredColors = new[] { "blue" } },
            },
        };

        var result = await Sut().GenerateAiOutfitAsync(_userId, start.Id, options);

        Assert.Contains(result.Warnings, w => w.Contains("blue") && w.Contains("bottoms"));
    }

    [Fact]
    public async Task GenerateAiOutfit_NoWarnings_WhenConstraintSatisfied()
    {
        var start = Start();
        var blueBottom = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom, Name = "b", ProcessedImageUrl = "b", Color = "blue", Embedding = new[] { 0f, 1f, 0f } };
        GivenCandidates((blueBottom, 0.9));

        var options = new OutfitGenerationOptions
        {
            Threshold = 0.4,
            GarmentConstraints = new Dictionary<ClothingType, GarmentSpec>
            {
                [ClothingType.Bottom] = new GarmentSpec { Type = ClothingType.Bottom, DesiredColors = new[] { "blue" } },
            },
        };

        var result = await Sut().GenerateAiOutfitAsync(_userId, start.Id, options);

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task GenerateAiOutfit_Throws_WhenStartItemMissing()
    {
        _clothing.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ClothingItem?)null);
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => Sut().GenerateAiOutfitAsync(_userId, Guid.NewGuid(), new OutfitGenerationOptions()));
    }

    [Fact]
    public async Task GenerateAiOutfit_Throws_WhenStartItemHasNoEmbedding()
    {
        var start = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top, Embedding = null };
        _clothing.GetByIdAsync(start.Id, Arg.Any<CancellationToken>()).Returns(start);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Sut().GenerateAiOutfitAsync(_userId, start.Id, new OutfitGenerationOptions()));
    }
}
