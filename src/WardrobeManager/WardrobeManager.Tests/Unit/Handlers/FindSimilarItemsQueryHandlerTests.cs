using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing.Queries;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public sealed class FindSimilarItemsQueryHandlerTests
{
    private readonly IClothingRepository _clothing = Substitute.For<IClothingRepository>();

    private FindSimilarItemsQueryHandler Sut() => new(_clothing);

    private static ClothingItem WithEmbedding(Guid userId, Guid? id = null)
    {
        var item = TestData.Item(id: id, userId: userId);
        item.Embedding = [0.1f, 0.2f, 0.3f];
        return item;
    }

    [Fact]
    public async Task Handle_ExcludesSeedAndRespectsLimit()
    {
        var userId = Guid.NewGuid();
        var seed = WithEmbedding(userId);
        var neighbour1 = WithEmbedding(userId);
        var neighbour2 = WithEmbedding(userId);
        var neighbour3 = WithEmbedding(userId);

        _clothing.GetByIdAsync(seed.Id, Arg.Any<CancellationToken>()).Returns(seed);
        // repository returns the seed itself first (cosine ~1.0), then the real neighbours.
        _clothing.GetSimilarItemsAsync(userId, seed.Embedding!, type: null, limit: 3, threshold: null, gender: null, ct: Arg.Any<CancellationToken>())
            .Returns([(seed, 1.0), (neighbour1, 0.92), (neighbour2, 0.88), (neighbour3, 0.81)]);

        var result = await Sut().Handle(new FindSimilarItemsQuery(userId, seed.Id, Limit: 2), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, r => r.Item.Id == seed.Id);
        Assert.Equal(neighbour1.Id, result[0].Item.Id);
        Assert.Equal(0.92, result[0].Similarity);
    }

    [Fact]
    public async Task Handle_ReturnsEmpty_WhenItemNotOwned()
    {
        var seed = WithEmbedding(Guid.NewGuid());
        _clothing.GetByIdAsync(seed.Id, Arg.Any<CancellationToken>()).Returns(seed);

        var result = await Sut().Handle(new FindSimilarItemsQuery(Guid.NewGuid(), seed.Id), CancellationToken.None);

        Assert.Empty(result);
        await _clothing.DidNotReceive().GetSimilarItemsAsync(
            Arg.Any<Guid>(), Arg.Any<float[]>(), Arg.Any<ClothingType?>(), Arg.Any<int>(), Arg.Any<double?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsEmpty_WhenNoEmbedding()
    {
        var userId = Guid.NewGuid();
        var seed = TestData.Item(userId: userId); // no embedding set
        _clothing.GetByIdAsync(seed.Id, Arg.Any<CancellationToken>()).Returns(seed);

        var result = await Sut().Handle(new FindSimilarItemsQuery(userId, seed.Id), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_PassesSeedType_WhenSameTypeOnly()
    {
        var userId = Guid.NewGuid();
        var seed = WithEmbedding(userId);
        seed.Type = ClothingType.Shoes;
        _clothing.GetByIdAsync(seed.Id, Arg.Any<CancellationToken>()).Returns(seed);
        _clothing.GetSimilarItemsAsync(userId, seed.Embedding!, type: ClothingType.Shoes, limit: Arg.Any<int>(), threshold: null, gender: null, ct: Arg.Any<CancellationToken>())
            .Returns([(seed, 1.0)]);

        await Sut().Handle(new FindSimilarItemsQuery(userId, seed.Id, SameTypeOnly: true), CancellationToken.None);

        await _clothing.Received(1).GetSimilarItemsAsync(
            userId, seed.Embedding!, type: ClothingType.Shoes, limit: Arg.Any<int>(), threshold: null, gender: null, ct: Arg.Any<CancellationToken>());
    }
}
