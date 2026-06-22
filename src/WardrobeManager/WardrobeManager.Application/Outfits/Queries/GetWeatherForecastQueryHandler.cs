using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Outfits.Queries;

public sealed class GetWeatherForecastQueryHandler(IWeatherService weatherService)
    : IRequestHandler<GetWeatherForecastQuery, List<DailyForecast>>
{
    public Task<List<DailyForecast>> Handle(GetWeatherForecastQuery request, CancellationToken ct)
    {
        return weatherService.GetForecastAsync(request.City, request.Days, request.StartDate, ct);
    }
}
