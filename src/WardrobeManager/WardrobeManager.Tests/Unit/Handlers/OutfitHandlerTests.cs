using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Commands;
using WardrobeManager.Application.Outfits.Learning;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public sealed class CreateOutfitCommandHandlerTests
{
    private readonly IOutfitRepository _outfits = Substitute.For<IOutfitRepository>();
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();

    private CreateOutfitCommandHandler Sut() => new(_outfits, _clothing);

    [Fact]
    public async Task Handle_CreatesOutfit_FromFetchedItems()
    {
        var itemId = Guid.NewGuid();
        var cmd = new CreateOutfitCommand(Guid.NewGuid(), "Look", new List<Guid> { itemId });
        var items = new List<ClothingItem> { new() { Id = itemId, Type = ClothingType.Top } };
        _clothing.GetByIdsForUserAsync(
            Arg.Is<IEnumerable<Guid>>(values => values.SequenceEqual(cmd.ItemIds)),
            cmd.UserId,
            Arg.Any<CancellationToken>()).Returns(items);

        var id = await Sut().Handle(cmd, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        await _outfits.Received(1).AddAsync(
            Arg.Is<Outfit>(o => o.Name == "Look" && o.Items.Count == 1), Arg.Any<CancellationToken>());
    }
}

[Trait("Category", "Unit")]
public sealed class DeleteOutfitCommandHandlerTests
{
    private readonly IOutfitRepository _outfits = Substitute.For<IOutfitRepository>();
    private DeleteOutfitCommandHandler Sut() => new(_outfits);

    [Fact]
    public async Task Handle_ReturnsTrue_AndDeletes_WhenFound()
    {
        var userId = Guid.NewGuid();
        var outfit = new Outfit { Id = Guid.NewGuid(), UserId = userId };
        _outfits.GetByIdForUserAsync(outfit.Id, userId, Arg.Any<CancellationToken>()).Returns(outfit);

        Assert.True(await Sut().Handle(new DeleteOutfitCommand(userId, outfit.Id), CancellationToken.None));
        await _outfits.Received(1).DeleteAsync(outfit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsFalse_WhenNotFound()
    {
        _outfits.GetByIdForUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Outfit?)null);
        Assert.False(await Sut().Handle(new DeleteOutfitCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }
}

[Trait("Category", "Unit")]
public sealed class UpdateOutfitCommandHandlerTests
{
    private readonly IOutfitRepository _outfits = Substitute.For<IOutfitRepository>();
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private UpdateOutfitCommandHandler Sut() => new(_outfits, _clothing);

    [Fact]
    public async Task Handle_UpdatesNameAndItems()
    {
        var userId = Guid.NewGuid();
        var outfit = new Outfit { Id = Guid.NewGuid(), UserId = userId, Name = "Old" };
        _outfits.GetByIdForUserAsync(outfit.Id, userId, Arg.Any<CancellationToken>()).Returns(outfit);
        var top = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top };
        var bottom = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Bottom };
        var ids = new List<Guid> { top.Id, bottom.Id };
        _clothing.GetByIdsForUserAsync(
            Arg.Is<IEnumerable<Guid>>(values => values.SequenceEqual(ids)),
            userId,
            Arg.Any<CancellationToken>()).Returns(new List<ClothingItem> { top, bottom });

        var result = await Sut().Handle(new UpdateOutfitCommand(userId, outfit.Id, "New", ids), CancellationToken.None);

        Assert.True(result);
        Assert.Equal("New", outfit.Name);
        Assert.Equal(2, outfit.Items.Count);
        await _outfits.Received(1).UpdateAsync(outfit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsFalse_WhenNotFound()
    {
        _outfits.GetByIdForUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Outfit?)null);
        Assert.False(await Sut().Handle(new UpdateOutfitCommand(Guid.NewGuid(), Guid.NewGuid(), "x", new List<Guid> { Guid.NewGuid() }), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Throws_OnDuplicateType()
    {
        var userId = Guid.NewGuid();
        var outfit = new Outfit { Id = Guid.NewGuid(), UserId = userId };
        _outfits.GetByIdForUserAsync(outfit.Id, userId, Arg.Any<CancellationToken>()).Returns(outfit);
        var top1 = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top };
        var top2 = new ClothingItem { Id = Guid.NewGuid(), Type = ClothingType.Top };
        var ids = new List<Guid> { top1.Id, top2.Id };
        _clothing.GetByIdsForUserAsync(
            Arg.Is<IEnumerable<Guid>>(values => values.SequenceEqual(ids)),
            userId,
            Arg.Any<CancellationToken>()).Returns(new List<ClothingItem> { top1, top2 });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Sut().Handle(new UpdateOutfitCommand(userId, outfit.Id, "x", ids), CancellationToken.None));
    }
}

[Trait("Category", "Unit")]
public sealed class ToggleOutfitFavoriteCommandHandlerTests
{
    private readonly IOutfitRepository _outfits = Substitute.For<IOutfitRepository>();
    private readonly IOutfitFeedbackRepository _feedback = Substitute.For<IOutfitFeedbackRepository>();
    private readonly IFeedbackLearningCoordinator _learning = Substitute.For<IFeedbackLearningCoordinator>();
    private ToggleOutfitFavoriteCommandHandler Sut() =>
        new(_outfits, _feedback, _learning, NullLogger<ToggleOutfitFavoriteCommandHandler>.Instance);

    [Fact]
    public async Task Handle_FlipsFavoriteFlag()
    {
        var userId = Guid.NewGuid();
        var outfit = new Outfit { Id = Guid.NewGuid(), UserId = userId, IsFavorite = false };
        _outfits.GetByIdForUserAsync(outfit.Id, userId, Arg.Any<CancellationToken>()).Returns(outfit);

        var result = await Sut().Handle(new ToggleOutfitFavoriteCommand(userId, outfit.Id), CancellationToken.None);

        Assert.True(result);
        Assert.True(outfit.IsFavorite);
        await _outfits.Received(1).UpdateAsync(outfit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmitsFavoritedFeedback_WhenFavoritingGeneratedOutfit()
    {
        var generationId = Guid.NewGuid();
        var outfit = new Outfit
        {
            Id = Guid.NewGuid(), UserId = Guid.NewGuid(), IsFavorite = false, AiGenerationId = generationId,
            Items = new() { new ClothingItem { Id = Guid.NewGuid() } },
        };
        _outfits.GetByIdForUserAsync(outfit.Id, outfit.UserId, Arg.Any<CancellationToken>()).Returns(outfit);

        await Sut().Handle(new ToggleOutfitFavoriteCommand(outfit.UserId, outfit.Id), CancellationToken.None);

        await _feedback.Received(1).RecordActionsForItemsAsync(outfit.UserId, generationId, Arg.Any<IEnumerable<Guid>>(), FeedbackAction.Favorited, Arg.Any<CancellationToken>());
        await _learning.Received(1).LearnFromGenerationAsync(outfit.UserId, generationId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SkipsFeedback_WhenUnfavoriting()
    {
        var userId = Guid.NewGuid();
        var outfit = new Outfit { Id = Guid.NewGuid(), UserId = userId, IsFavorite = true, AiGenerationId = Guid.NewGuid(), Items = new() };
        _outfits.GetByIdForUserAsync(outfit.Id, userId, Arg.Any<CancellationToken>()).Returns(outfit);

        await Sut().Handle(new ToggleOutfitFavoriteCommand(userId, outfit.Id), CancellationToken.None);

        await _learning.DidNotReceive().LearnFromGenerationAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenNotFound()
    {
        _outfits.GetByIdForUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Outfit?)null);
        Assert.Null(await Sut().Handle(new ToggleOutfitFavoriteCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }
}

[Trait("Category", "Unit")]
public sealed class GetOutfitsQueryHandlerTests
{
    private readonly IOutfitRepository _outfits = Substitute.For<IOutfitRepository>();
    private GetOutfitsQueryHandler Sut() => new(_outfits);

    [Fact]
    public async Task Handle_ExcludesEventExclusiveOutfits()
    {
        var userId = Guid.NewGuid();
        var visible = new Outfit { Id = Guid.NewGuid(), Name = "Visible", IsEventExclusive = false, Items = new() };
        var hidden = new Outfit { Id = Guid.NewGuid(), Name = "Hidden", IsEventExclusive = true, Items = new() };
        _outfits.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<Outfit> { visible, hidden });

        var result = await Sut().Handle(new GetOutfitsQuery(userId), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Visible", result[0].Name);
    }
}
