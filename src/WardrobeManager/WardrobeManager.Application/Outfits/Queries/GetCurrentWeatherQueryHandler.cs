using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Outfits.Queries;

public sealed class GetCurrentWeatherQueryHandler(IWeatherService weatherService)
    : IRequestHandler<GetCurrentWeatherQuery, WeatherData>
{
    public Task<WeatherData> Handle(GetCurrentWeatherQuery request, CancellationToken ct)
    {
        return weatherService.GetCurrentWeatherAsync(request.City, ct);
    }
}
