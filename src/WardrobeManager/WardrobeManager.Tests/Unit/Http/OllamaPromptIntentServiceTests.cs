using System.Net;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Enums;
using WardrobeManager.Infrastructure.ExternalServices;

namespace WardrobeManager.Tests.Unit.Http;

[Trait("Category", "Unit")]
public sealed class OllamaPromptIntentServiceTests
{
    private readonly IMlService _ml = Substitute.For<IMlService>();

    private static IConfiguration Config()
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

    private OllamaPromptIntentService Service(FakeHttpMessageHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434/") }, Config(), _ml);

    [Fact]
    public async Task ParseAsync_ReturnsEmptyIntent_ForBlankPrompt()
    {
        var result = await Service(new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(new { }))).ParseAsync("  ");
        Assert.Null(result.Style);
    }

    [Fact]
    public async Task ParseAsync_CoercesModelOutput()
    {
        const string intentJson =
            """{"style":"Formal","city":"Paris","occasion":"wedding","desiredColors":["Black"],"avoidColors":[],"anchorDescription":"black shirt","requestedTypes":["Top","Top"],"formality":5,"temperatureHint":"Mild"}""";
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(new
        {
            message = new { role = "assistant", content = intentJson },
        }));

        var result = await Service(handler).ParseAsync("ceva elegant pentru o nunta");

        Assert.Equal("Formal", result.Style);
        Assert.Equal("Paris", result.City);
        Assert.Equal(new[] { "black" }, result.DesiredColors);   // normalized lowercase
        Assert.Equal(new[] { ClothingType.Top }, result.RequestedTypes); // deduped
        Assert.Equal(5, result.Formality);
        Assert.Equal("mild", result.TemperatureHint);
    }

    [Fact]
    public async Task ParseAsync_CoercesPerGarmentColors_AndClearsGlobalsForBoundColors()
    {
        const string intentJson =
            """{"style":null,"city":null,"occasion":null,"desiredColors":["black"],"avoidColors":[],"anchorDescription":"t-shirt","requestedTypes":["Top","Bottom"],"garments":[{"type":"Top","desiredColors":[],"avoidColors":["Black","White"]},{"type":"Bottom","desiredColors":["black"],"avoidColors":[]}],"formality":null,"temperatureHint":null}""";
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(new
        {
            message = new { role = "assistant", content = intentJson },
        }));

        var result = await Service(handler).ParseAsync("un tricou care sa nu fie negru sau alb, si pantaloni negri");

        var top = result.GarmentSpecs.Single(g => g.Type == ClothingType.Top);
        Assert.Equal(new[] { "black", "white" }, top.AvoidColors); // normalized lowercase
        Assert.Empty(top.DesiredColors);

        var bottom = result.GarmentSpecs.Single(g => g.Type == ClothingType.Bottom);
        Assert.Equal(new[] { "black" }, bottom.DesiredColors);

        // "black" was bound to the bottom -> dropped from the global list so it isn't vetoed everywhere.
        Assert.Empty(result.DesiredColors);
        Assert.Empty(result.AvoidColors);
    }

    [Fact]
    public async Task ParseAsync_FallsBackToMl_OnHttpError()
    {
        _ml.ParsePromptAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Returns(("Sports", 0.8d, (string?)"London"));
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Status(HttpStatusCode.InternalServerError));

        var result = await Service(handler).ParseAsync("something for the gym");

        Assert.Equal("Sports", result.Style);
        Assert.Equal("London", result.City);
    }

    [Fact]
    public async Task ParseAsync_FallsBackToMl_WhenContentEmpty()
    {
        _ml.ParsePromptAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Returns(("Casual", 0.5d, (string?)null));
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(new
        {
            message = new { role = "assistant", content = "" },
        }));

        var result = await Service(handler).ParseAsync("whatever");

        Assert.Equal("Casual", result.Style);
        Assert.Null(result.City);
    }
}
