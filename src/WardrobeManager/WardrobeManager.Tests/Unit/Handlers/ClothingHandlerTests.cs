using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing.Commands;
using WardrobeManager.Application.Clothing.Queries;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public sealed class AddClothingCommandHandlerTests
{
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private AddClothingCommandHandler Sut() => new(_clothing, _users);

    private static AddClothingCommand Command(string image = "rawbase64")
        => new(Guid.NewGuid(), "Blue Shirt", ClothingType.Top, "tshirts", "blue", "Men", "Summer", "Casual", image, null);

    [Fact]
    public async Task Handle_PersistsItem_AndPrefixesRawBase64()
    {
        var cmd = Command("rawbase64");
        _users.GetByIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(new User { Id = cmd.UserId });

        var result = await Sut().Handle(cmd, CancellationToken.None);

        Assert.Equal("data:image/png;base64,rawbase64", result.ProcessedImageUrl);
        Assert.Equal("Blue Shirt", result.Name);
        await _clothing.Received(1).AddAsync(Arg.Any<ClothingItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_KeepsDataUri_Untouched()
    {
        var cmd = Command("data:image/png;base64,abc");
        _users.GetByIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(new User { Id = cmd.UserId });

        var result = await Sut().Handle(cmd, CancellationToken.None);

        Assert.Equal("data:image/png;base64,abc", result.ProcessedImageUrl);
    }

    [Fact]
    public async Task Handle_Throws_WhenUserNotFound()
    {
        var cmd = Command();
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().Handle(cmd, CancellationToken.None));
        await _clothing.DidNotReceive().AddAsync(Arg.Any<ClothingItem>(), Arg.Any<CancellationToken>());
    }
}

[Trait("Category", "Unit")]
public sealed class DeleteClothingCommandHandlerTests
{
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private DeleteClothingCommandHandler Sut() => new(_clothing);

    [Fact]
    public async Task Handle_DeletesExistingItem()
    {
        var userId = Guid.NewGuid();
        var item = new ClothingItem { Id = Guid.NewGuid(), UserId = userId };
        _clothing.GetByIdForUserAsync(item.Id, userId, Arg.Any<CancellationToken>()).Returns(item);

        var result = await Sut().Handle(new DeleteClothingCommand(userId, item.Id), CancellationToken.None);

        Assert.True(result);
        await _clothing.Received(1).DeleteAsync(item, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsFalse_WhenItemNotFoundOrNotOwned()
    {
        _clothing.GetByIdForUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ClothingItem?)null);
        Assert.False(await Sut().Handle(new DeleteClothingCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }
}

[Trait("Category", "Unit")]
public sealed class GetClothingItemsQueryHandlerTests
{
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private GetClothingItemsQueryHandler Sut() => new(_clothing);

    [Fact]
    public async Task Handle_MapsItemsToDtos()
    {
        var userId = Guid.NewGuid();
        var items = new List<ClothingItem>
        {
            new() { Id = Guid.NewGuid(), Name = "Shirt", Type = ClothingType.Top, ProcessedImageUrl = "url1" },
            new() { Id = Guid.NewGuid(), Name = "Jeans", Type = ClothingType.Bottom, ProcessedImageUrl = null },
        };
        _clothing.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(items);

        var result = await Sut().Handle(new GetClothingItemsQuery(userId), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Shirt", result[0].Name);
        Assert.Equal(string.Empty, result[1].ProcessedImageUrl); // null coalesced
    }
}
