using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using WardrobeManager.Infrastructure.ExternalServices;

namespace WardrobeManager.Tests.Unit.Http;

[Trait("Category", "Unit")]
public sealed class MlServiceTests
{
    private static MlService Service(Func<HttpRequestMessage, HttpResponseMessage> responder)
        => new(new HttpClient(new FakeHttpMessageHandler(responder)) { BaseAddress = new Uri("http://ml-api/") },
               NullLogger<MlService>.Instance);

    [Fact]
    public async Task ProcessClothingImage_MapsResponse()
    {
        var sut = Service(_ => FakeHttpMessageHandler.Json(new
        {
            type = "tshirts", color = "blue", processed_image_b64 = "b64",
            embedding = new[] { 0.1f, 0.2f }, gender = "Men", season = "Summer", usage = "Casual",
        }));

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("img"));
        var result = await sut.ProcessClothingImageAsync(stream, "f.png", "image/png");

        Assert.Equal("tshirts", result.Type);
        Assert.Equal("blue", result.Color);
        Assert.Equal("Men", result.Gender);
        Assert.Equal(2, result.Embedding!.Length);
    }

    [Fact]
    public async Task ProcessClothingImage_Throws_OnError()
    {
        var sut = Service(_ => FakeHttpMessageHandler.Status(HttpStatusCode.BadGateway));
        using var stream = new MemoryStream(new byte[] { 1 });
        await Assert.ThrowsAsync<Exception>(() => sut.ProcessClothingImageAsync(stream, "f.png", "image/png"));
    }

    [Fact]
    public async Task ParsePrompt_ReturnsParsedFields()
    {
        var sut = Service(_ => FakeHttpMessageHandler.Json(new { style = "Formal", styleConfidence = 0.9, city = "Rome" }));
        var (style, confidence, city) = await sut.ParsePromptAsync("elegant");
        Assert.Equal("Formal", style);
        Assert.Equal(0.9, confidence);
        Assert.Equal("Rome", city);
    }

    [Fact]
    public async Task ParsePrompt_ReturnsDefault_OnError()
    {
        var sut = Service(_ => FakeHttpMessageHandler.Status(HttpStatusCode.InternalServerError));
        var (style, confidence, city) = await sut.ParsePromptAsync("x");
        Assert.Equal("Casual", style);
        Assert.Equal(0, confidence);
        Assert.Null(city);
    }

    [Fact]
    public async Task EmbedText_ReturnsEmbedding_AndEmptyOnError()
    {
        var ok = Service(_ => FakeHttpMessageHandler.Json(new { embedding = new[] { 1f, 2f, 3f } }));
        Assert.Equal(3, (await ok.EmbedTextAsync("hi")).Length);

        var err = Service(_ => FakeHttpMessageHandler.Status(HttpStatusCode.BadRequest));
        Assert.Empty(await err.EmbedTextAsync("hi"));
    }

    [Fact]
    public async Task PredictArticleTypes_ShortCircuits_OnEmptyInput()
    {
        var sut = Service(_ => FakeHttpMessageHandler.Status(HttpStatusCode.InternalServerError));
        Assert.Empty(await sut.PredictArticleTypesAsync(Array.Empty<float[]>()));
    }

    [Fact]
    public async Task PredictArticleTypes_ReturnsTypes()
    {
        var sut = Service(_ => FakeHttpMessageHandler.Json(new { types = new[] { "jeans", "tshirts" } }));
        var result = await sut.PredictArticleTypesAsync(new[] { new[] { 1f } });
        Assert.Equal(new[] { "jeans", "tshirts" }, result);
    }

    [Fact]
    public async Task GetArticleTypes_ReturnsTypes_AndEmptyOnError()
    {
        var ok = Service(_ => FakeHttpMessageHandler.Json(new { types = new[] { "shorts" } }));
        Assert.Single(await ok.GetArticleTypesAsync());

        var err = Service(_ => FakeHttpMessageHandler.Status(HttpStatusCode.NotFound));
        Assert.Empty(await err.GetArticleTypesAsync());
    }
}
