using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing.Commands;
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
        _clothing.GetByIdForUserAsync(item.Id, userId, Arg.Any<CancellationToken>()).Returns(item);

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
        _clothing.GetByIdForUserAsync(item.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ClothingItem?)null);

        var cmd = new UpdateClothingCommand(item.Id, Guid.NewGuid(), "n", ClothingType.Top, null, null, null, null);
        Assert.False(await Sut().Handle(cmd, CancellationToken.None));
    }
}

[Trait("Category", "Unit")]
public sealed class ProcessClothingCommandHandlerTests
{
    private readonly IMlService _ml = Substitute.For<IMlService>();
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();
    private readonly INotificationDispatcher _notifications = Substitute.For<INotificationDispatcher>();
    private ProcessClothingCommandHandler Sut() => new(_ml, _clothing, _notifications, NullLogger<ProcessClothingCommandHandler>.Instance);

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
        Assert.Empty(result.PossibleDuplicates); // no similar items configured on the mock
    }

    [Fact]
    public async Task Handle_SurfacesDuplicates_WhenSimilarItemsExist()
    {
        var userId = Guid.NewGuid();
        _ml.ProcessClothingImageAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new MlClothingResult("tshirts", "black", "b64", new[] { 0.1f, 0.2f }, "Men", "Summer", "Casual"));
        var existing = new ClothingItem { Id = Guid.NewGuid(), UserId = userId, Name = "Black tee", ProcessedImageUrl = "img" };
        _clothing.GetSimilarItemsAsync(userId, Arg.Any<float[]>(), Arg.Any<ClothingType?>(), Arg.Any<int>(), Arg.Any<double?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns([(existing, 0.95)]);

        var cmd = new ProcessClothingCommand(new byte[] { 1 }, "f.png", "image/png", userId, "Another black tee");
        var result = await Sut().Handle(cmd, CancellationToken.None);

        Assert.Single(result.PossibleDuplicates);
        Assert.Equal(existing.Id, result.PossibleDuplicates[0].Id);
    }
}
