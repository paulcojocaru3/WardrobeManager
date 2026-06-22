using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Outfits.Queries;

public sealed record GetWeatherForecastQuery(string City, int Days, DateTime? StartDate)
    : IRequest<List<DailyForecast>>;
