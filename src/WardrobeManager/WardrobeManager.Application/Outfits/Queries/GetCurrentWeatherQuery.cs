using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Outfits.Queries;

public sealed record GetCurrentWeatherQuery(string City) : IRequest<WeatherData>;
