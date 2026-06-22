using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;
using WardrobeManager.Infrastructure.Repositories;

namespace WardrobeManager.Tests.Integration;

[Trait("Category", "Integration")]
[Collection(PostgresCollection.Name)]
public sealed class ClothingRepositoryTests
{
    private const int EmbeddingDims = 512; // Fashion-CLIP dimensionality (vector(512))
    private readonly PostgresContainerFixture _fixture;

    public ClothingRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetSimilarItemsAsync_OrdersByCosineSimilarity_MostSimilarFirst()
    {
        var userId = await SeedUserAsync();

        // query along axis 0. Similarity to the query: aligned (1.0) > diagonal (~0.707) > orthogonal (0.0).
        var aligned = await SeedItemAsync(userId, "aligned", Embed((0, 1f)));
        var diagonal = await SeedItemAsync(userId, "diagonal", Embed((0, 1f), (1, 1f)));
        var orthogonal = await SeedItemAsync(userId, "orthogonal", Embed((1, 1f)));

        var query = Embed((0, 1f));
        List<(ClothingItem Item, double Similarity)> results;
        await using (var context = _fixture.CreateContext())
        {
            var repo = new ClothingRepository(context);
            results = await repo.GetSimilarItemsAsync(userId, query);
        }

        Assert.Equal(
            new[] { aligned, diagonal, orthogonal },
            results.Select(r => r.Item.Id).ToArray());

        // similarity is 1 - cosine distance: exact for the aligned/orthogonal items.
        Assert.Equal(1.0, results[0].Similarity, precision: 5);
        Assert.Equal(0.707, results[1].Similarity, precision: 3);
        Assert.Equal(0.0, results[2].Similarity, precision: 5);
    }

    [Fact]
    public async Task GetSimilarItemsAsync_AppliesThreshold_DropsLowSimilarity()
    {
        var userId = await SeedUserAsync();
        var aligned = await SeedItemAsync(userId, "aligned", Embed((0, 1f)));
        await SeedItemAsync(userId, "orthogonal", Embed((1, 1f))); // similarity 0 — below threshold

        List<(ClothingItem Item, double Similarity)> results;
        await using (var context = _fixture.CreateContext())
        {
            var repo = new ClothingRepository(context);
            results = await repo.GetSimilarItemsAsync(userId, Embed((0, 1f)), threshold: 0.5);
        }

        Assert.Single(results);
        Assert.Equal(aligned, results[0].Item.Id);
    }

    [Fact]
    public async Task GetSimilarItemsAsync_FiltersByType()
    {
        var userId = await SeedUserAsync();
        var bottom = await SeedItemAsync(userId, "jeans", Embed((0, 1f)), type: ClothingType.Bottom);
        await SeedItemAsync(userId, "tshirt", Embed((0, 1f)), type: ClothingType.Top);

        List<(ClothingItem Item, double Similarity)> results;
        await using (var context = _fixture.CreateContext())
        {
            var repo = new ClothingRepository(context);
            results = await repo.GetSimilarItemsAsync(userId, Embed((0, 1f)), type: ClothingType.Bottom);
        }

        Assert.Single(results);
        Assert.Equal(bottom, results[0].Item.Id);
    }

    [Fact]
    public async Task GetSimilarItemsAsync_GenderFilter_KeepsSameGenderUnisexAndUnspecified()
    {
        var userId = await SeedUserAsync();
        var men = await SeedItemAsync(userId, "men", Embed((0, 1f)), gender: "Men");
        var unisex = await SeedItemAsync(userId, "unisex", Embed((0, 1f)), gender: "Unisex");
        var unspecified = await SeedItemAsync(userId, "unspecified", Embed((0, 1f)), gender: null);
        await SeedItemAsync(userId, "women", Embed((0, 1f)), gender: "Women"); // filtered out

        List<(ClothingItem Item, double Similarity)> results;
        await using (var context = _fixture.CreateContext())
        {
            var repo = new ClothingRepository(context);
            results = await repo.GetSimilarItemsAsync(userId, Embed((0, 1f)), gender: "Men");
        }

        Assert.Equal(
            new[] { men, unisex, unspecified }.OrderBy(g => g),
            results.Select(r => r.Item.Id).OrderBy(g => g));
    }

    [Fact]
    public async Task GetSimilarItemsAsync_ScopesToOwningUser()
    {
        var owner = await SeedUserAsync();
        var stranger = await SeedUserAsync();
        var mine = await SeedItemAsync(owner, "mine", Embed((0, 1f)));
        await SeedItemAsync(stranger, "theirs", Embed((0, 1f)));

        List<(ClothingItem Item, double Similarity)> results;
        await using (var context = _fixture.CreateContext())
        {
            var repo = new ClothingRepository(context);
            results = await repo.GetSimilarItemsAsync(owner, Embed((0, 1f)));
        }

        Assert.Single(results);
        Assert.Equal(mine, results[0].Item.Id);
    }

    [Fact]
    public async Task GetSimilarItemsAsync_IgnoresItemsWithoutEmbedding()
    {
        var userId = await SeedUserAsync();
        var withEmbedding = await SeedItemAsync(userId, "embedded", Embed((0, 1f)));
        await SeedItemAsync(userId, "no-embedding", embedding: null);

        List<(ClothingItem Item, double Similarity)> results;
        await using (var context = _fixture.CreateContext())
        {
            var repo = new ClothingRepository(context);
            results = await repo.GetSimilarItemsAsync(userId, Embed((0, 1f)));
        }

        Assert.Single(results);
        Assert.Equal(withEmbedding, results[0].Item.Id);
    }

    // --- helpers ---

    private async Task<Guid> SeedUserAsync()
    {
        var user = new User
        {
            Username = $"u_{Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@test.local",
            PasswordHash = "x",
        };

        await using var context = _fixture.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    private async Task<Guid> SeedItemAsync(
        Guid userId,
        string name,
        float[]? embedding,
        ClothingType type = ClothingType.Top,
        string? gender = null)
    {
        var item = new ClothingItem
        {
            UserId = userId,
            Name = name,
            Type = type,
            Gender = gender,
            Embedding = embedding,
        };

        await using var context = _fixture.CreateContext();
        context.ClothingItems.Add(item);
        await context.SaveChangesAsync();
        return item.Id;
    }

    // sparse 512-d vector with the given (index, value) components set; rest zero.
    private static float[] Embed(params (int Index, float Value)[] components)
    {
        var vector = new float[EmbeddingDims];
        foreach (var (index, value) in components)
        {
            vector[index] = value;
        }
        return vector;
    }
}
