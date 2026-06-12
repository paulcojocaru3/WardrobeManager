using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using WardrobeManager.Infrastructure.ExternalServices;

namespace WardrobeManager.Tests.Unit.Http;

[Trait("Category", "Unit")]
public sealed class WeatherServiceTests
{
    private static IConfiguration Config(string? key = "real-key")
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["WeatherApi:Key"] = key })
            .Build();

    private static IMemoryCache Cache() => new MemoryCache(new MemoryCacheOptions());

    private static WeatherService Service(FakeHttpMessageHandler handler, string? key = "real-key")
        => new(new HttpClient(handler), Config(key), Cache());

    [Fact]
    public async Task GetCurrentWeather_MapsResponse_AndCaches()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(new
        {
            main = new { temp = 27.5 },
            weather = new[] { new { main = "Clouds" } },
        }));
        var sut = Service(handler);

        var first = await sut.GetCurrentWeatherAsync("Paris");
        var second = await sut.GetCurrentWeatherAsync("Paris"); // served from cache

        Assert.Equal(27.5f, first.Temperature);
        Assert.Equal("Clouds", first.Condition);
        Assert.Equal("Summer", first.SeasonSuggestion); // > 25
        Assert.Equal(1, handler.CallCount);              // cached, not re-fetched
    }

    [Fact]
    public async Task GetCurrentWeather_Throws_WhenApiKeyMissing()
    {
        var sut = Service(new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(new { })), key: null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetCurrentWeatherAsync("Paris"));
    }

    [Fact]
    public async Task SearchCities_ReturnsSuggestions()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(new[]
        {
            new { name = "Paris", country = "FR", state = (string?)null },
            new { name = "Paris", country = "US", state = "Texas" },
        }));

        var result = await Service(handler).SearchCitiesAsync("Paris");

        Assert.Equal(2, result.Count);
        Assert.Equal("FR", result[0].Country);
    }

    [Theory]
    [InlineData("a")]   // too short
    [InlineData("")]
    public async Task SearchCities_ReturnsEmpty_ForShortQuery(string query)
    {
        var result = await Service(new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(Array.Empty<object>())))
            .SearchCitiesAsync(query);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCities_ReturnsEmpty_OnHttpError()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Status(HttpStatusCode.InternalServerError));
        Assert.Empty(await Service(handler).SearchCitiesAsync("Paris"));
    }

    [Fact]
    public async Task GetForecast_BuildsPerDayForecast()
    {
        var today = DateTime.UtcNow.Date;
        var unix = new DateTimeOffset(today, TimeSpan.Zero).ToUnixTimeSeconds();
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(new
        {
            list = new[]
            {
                new { dt = unix, temp = new { day = 12.0 }, weather = new[] { new { main = "Rain" } } },
            },
        }));

        var result = await Service(handler).GetForecastAsync("Paris", 2);

        Assert.Equal(2, result.Count);
        Assert.Equal("Rain", result[0].Condition);       // matched day
        Assert.Equal("Spring", result[1].SeasonSuggestion); // fallback day uses last forecast (12 -> Spring)
    }

    [Fact]
    public async Task GetForecast_Throws_WhenApiKeyMissing()
    {
        var sut = Service(new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(new { })), key: null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetForecastAsync("Paris", 3));
    }

    private static FakeHttpMessageHandler OneDayForecast()
    {
        var unix = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).ToUnixTimeSeconds();
        return new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(new
        {
            list = new[] { new { dt = unix, temp = new { day = 20.0 }, weather = new[] { new { main = "Clear" } } } },
        }));
    }

    [Fact]
    public async Task GetForecast_ClampsExcessiveDayCount_To16()
    {
        var result = await Service(OneDayForecast()).GetForecastAsync("Paris", 50);

        Assert.Equal(16, result.Count); // clamped down from 50
    }

    [Fact]
    public async Task GetForecast_ClampsZeroDayCount_ToOne()
    {
        var result = await Service(OneDayForecast()).GetForecastAsync("Paris", 0);

        Assert.Single(result); // clamped up from 0 to 1
    }

    [Fact]
    public async Task GetForecast_Throws_WhenForecastListEmpty()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(new { list = Array.Empty<object>() }));

        await Assert.ThrowsAnyAsync<Exception>(() => Service(handler).GetForecastAsync("Paris", 3));
    }
}
