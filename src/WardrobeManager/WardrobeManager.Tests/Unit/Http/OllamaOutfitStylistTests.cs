using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Infrastructure.ExternalServices;

namespace WardrobeManager.Tests.Unit.Http;

[Trait("Category", "Unit")]
public sealed class OllamaOutfitStylistTests
{
    private HttpRequestMessage? _lastRequest;

    private OllamaOutfitStylist Service(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new FakeHttpMessageHandler(req => { _lastRequest = req; return responder(req); });
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Ollama:Model"] = "gemma3" })
            .Build();
        return new OllamaOutfitStylist(
            new HttpClient(handler) { BaseAddress = new Uri("http://ollama/") },
            config,
            NullLogger<OllamaOutfitStylist>.Instance);
    }

    // wraps the inner outfits json the way Ollama returns it: { message: { content: "<json>" } }
    private static HttpResponseMessage ChatReply(object outfitsPayload)
        => FakeHttpMessageHandler.Json(new { message = new { content = JsonSerializer.Serialize(outfitsPayload) } });

    private static IReadOnlyList<StylistItem> Candidates() => new[]
    {
        new StylistItem(1, "TOP", "blue cotton tee"),
        new StylistItem(2, "BOTTOM", "black chinos"),
        new StylistItem(3, "SHOES", "white sneakers"),
    };

    private static StylistContext Context() => new(Occasion: "everyday", TimeOfDay: null, WeatherSummary: null);

    [Fact]
    public async Task ComposeAsync_ReturnsNull_WhenNoCandidates()
    {
        var sut = Service(_ => FakeHttpMessageHandler.Status(HttpStatusCode.OK));

        var result = await sut.ComposeAsync(Array.Empty<StylistItem>(), Context());

        Assert.Null(result);
    }

    [Fact]
    public async Task ComposeAsync_MapsOutfits_FromValidResponse()
    {
        var sut = Service(_ => ChatReply(new
        {
            outfits = new[]
            {
                new { items = new[] { 1, 2, 3 }, headline = "Clean weekday", highlights = new[] { "neutral palette" }, styling_tip = "cuff the chinos" },
            }
        }));

        var result = await sut.ComposeAsync(Candidates(), Context());

        Assert.NotNull(result);
        var outfit = Assert.Single(result!);
        Assert.Equal(new[] { 1, 2, 3 }, outfit.ItemNumbers);
        Assert.Equal("Clean weekday", outfit.Headline);
        Assert.Equal("neutral palette", Assert.Single(outfit.Highlights));
        Assert.Equal("cuff the chinos", outfit.StylingTip);
    }

    [Fact]
    public async Task ComposeAsync_StripsApologeticText()
    {
        var sut = Service(_ => ChatReply(new
        {
            outfits = new[]
            {
                new
                {
                    items = new[] { 1, 2, 3 },
                    headline = "Sorry, no match found",
                    highlights = new[] { "couldn't find a better top", "clean lines" },
                    styling_tip = "closest match available",
                },
            }
        }));

        var result = await sut.ComposeAsync(Candidates(), Context());

        var outfit = Assert.Single(result!);
        Assert.Equal(string.Empty, outfit.Headline);
        Assert.Equal("clean lines", Assert.Single(outfit.Highlights));
        Assert.Equal(string.Empty, outfit.StylingTip);
    }

    [Fact]
    public async Task ComposeAsync_DeduplicatesItemNumbers()
    {
        var sut = Service(_ => ChatReply(new
        {
            outfits = new[] { new { items = new[] { 1, 1, 2, 3 }, headline = "x", highlights = new[] { "y" }, styling_tip = "z" } }
        }));

        var result = await sut.ComposeAsync(Candidates(), Context());

        Assert.Equal(new[] { 1, 2, 3 }, Assert.Single(result!).ItemNumbers);
    }

    [Fact]
    public async Task ComposeAsync_SkipsOutfitsWithoutItems()
    {
        var sut = Service(_ => ChatReply(new
        {
            outfits = new object[]
            {
                new { items = Array.Empty<int>(), headline = "empty", highlights = new[] { "a" }, styling_tip = "b" },
                new { items = new[] { 1, 2, 3 }, headline = "real", highlights = new[] { "a" }, styling_tip = "b" },
            }
        }));

        var result = await sut.ComposeAsync(Candidates(), Context());

        Assert.Equal("real", Assert.Single(result!).Headline);
    }

    [Fact]
    public async Task ComposeAsync_ReturnsNull_WhenOutfitsListEmpty()
    {
        var sut = Service(_ => ChatReply(new { outfits = Array.Empty<object>() }));

        Assert.Null(await sut.ComposeAsync(Candidates(), Context()));
    }

    [Fact]
    public async Task ComposeAsync_ReturnsNull_WhenContentBlank()
    {
        var sut = Service(_ => FakeHttpMessageHandler.Json(new { message = new { content = "  " } }));

        Assert.Null(await sut.ComposeAsync(Candidates(), Context()));
    }

    [Fact]
    public async Task ComposeAsync_ReturnsNull_OnHttpError()
    {
        var sut = Service(_ => FakeHttpMessageHandler.Status(HttpStatusCode.InternalServerError));

        Assert.Null(await sut.ComposeAsync(Candidates(), Context()));
    }

    [Fact]
    public async Task ComposeAsync_BuildsRichMessage_FromFullContext()
    {
        var sut = Service(_ => ChatReply(new
        {
            outfits = new[] { new { items = new[] { 1, 2, 3 }, headline = "h", highlights = new[] { "a" }, styling_tip = "t" } }
        }));
        var context = new StylistContext(
            Occasion: "wedding",
            TimeOfDay: "evening",
            WeatherSummary: "18C clear",
            AllowOuterwear: false,
            Style: "smart casual",
            MandatoryItemNumber: 2,
            MandatorySlot: "BOTTOM",
            Shuffle: true,
            FavoriteColors: new[] { "navy" },
            AvoidColors: new[] { "orange" });

        var result = await sut.ComposeAsync(Candidates(), context);

        Assert.NotNull(result);
        var body = await _lastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("wedding", body);
        Assert.Contains("MANDATORY", body);
        Assert.Contains("navy", body);
        Assert.Contains("orange", body);
        Assert.Contains("Do NOT include any OUTERWEAR", body);
    }

    [Fact]
    public async Task RepairAsync_ReturnsNull_WhenNoCandidates()
    {
        var sut = Service(_ => FakeHttpMessageHandler.Status(HttpStatusCode.OK));

        var result = await sut.RepairAsync(
            Array.Empty<StylistItem>(), Context(),
            new[] { new StylistOutfit(new[] { 9 }, "bad", Array.Empty<string>(), "") },
            "missing shoes");

        Assert.Null(result);
    }

    [Fact]
    public async Task RepairAsync_ResendsAndMapsFixedOutfit()
    {
        var sut = Service(_ => ChatReply(new
        {
            outfits = new[] { new { items = new[] { 1, 2, 3 }, headline = "fixed", highlights = new[] { "a" }, styling_tip = "t" } }
        }));

        var result = await sut.RepairAsync(
            Candidates(), Context(),
            new[] { new StylistOutfit(new[] { 1, 2 }, "bad", new[] { "h" }, "tip") },
            "missing shoes");

        Assert.Equal("fixed", Assert.Single(result!).Headline);
        var body = await _lastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("REPAIR TASK", body);
        Assert.Contains("missing shoes", body);
    }
}
