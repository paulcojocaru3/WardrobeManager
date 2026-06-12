using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing.Commands;
using WardrobeManager.Application.Clothing.Validators;
using WardrobeManager.Application.Seeding.Commands;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public sealed class UpdateClothingCommandHandlerTests
{
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private UpdateClothingCommandHandler Sut() => new(_clothing);

    [Fact]
    public async Task Handle_UpdatesOwnedItem()
    {
        var userId = Guid.NewGuid();
        var item = new ClothingItem { Id = Guid.NewGuid(), UserId = userId, Name = "Old" };
        _clothing.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var cmd = new UpdateClothingCommand(item.Id, userId, "New", ClothingType.Bottom, "blue", "Men", "Summer", "Casual", " JEANS ");
        var result = await Sut().Handle(cmd, CancellationToken.None);

        Assert.True(result);
        Assert.Equal("New", item.Name);
        Assert.Equal("jeans", item.SubType); // trimmed + lowercased
        await _clothing.Received(1).UpdateAsync(item, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsFalse_WhenNotOwned()
    {
        var item = new ClothingItem { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        _clothing.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var cmd = new UpdateClothingCommand(item.Id, Guid.NewGuid(), "n", ClothingType.Top, null, null, null, null);
        Assert.False(await Sut().Handle(cmd, CancellationToken.None));
    }
}

[Trait("Category", "Unit")]
public sealed class ProcessClothingCommandHandlerTests
{
    private readonly IMlService _ml = Substitute.For<IMlService>();
    private ProcessClothingCommandHandler Sut() => new(_ml, new ProcessClothingCommandValidator());

    [Fact]
    public async Task Handle_MapsMlResult_ToDto()
    {
        _ml.ProcessClothingImageAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new MlClothingResult("tshirts", "blue", "b64data", new[] { 0.1f }, "Men", "Summer", "Casual"));

        var cmd = new ProcessClothingCommand(new byte[] { 1, 2 }, "f.png", "image/png", Guid.NewGuid(), "My Shirt");
        var result = await Sut().Handle(cmd, CancellationToken.None);

        Assert.Equal("My Shirt", result.Name);
        Assert.Equal("blue", result.Color);
        Assert.Equal("b64data", result.ProcessedImageB64);
    }
}

[Trait("Category", "Unit")]
public sealed class SeedWearEventsCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private readonly IOutfitRepository _outfits = Substitute.For<IOutfitRepository>();
    private readonly IWearEventRepository _wear = Substitute.For<IWearEventRepository>();

    private SeedWearEventsCommandHandler Sut() => new(_users, _clothing, _outfits, _wear);

    [Fact]
    public async Task Handle_SeedsHistory_ForUserWithClothes()
    {
        var userId = Guid.NewGuid();
        _users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new User { Id = userId, Username = "alice" });
        _clothing.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<ClothingItem> { new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() } });
        _outfits.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<Outfit>());

        var result = await Sut().Handle(new SeedWearEventsCommand(userId), CancellationToken.None);

        Assert.Equal("alice", result.Username);
        Assert.True(result.EventsAdded > 0);
        await _wear.Received(1).AddRangeAsync(Arg.Any<IEnumerable<WearEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenUserMissing()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Sut().Handle(new SeedWearEventsCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Throws_WhenNoClothes()
    {
        var userId = Guid.NewGuid();
        _users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new User { Id = userId });
        _clothing.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<ClothingItem>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Sut().Handle(new SeedWearEventsCommand(userId), CancellationToken.None));
    }
}
