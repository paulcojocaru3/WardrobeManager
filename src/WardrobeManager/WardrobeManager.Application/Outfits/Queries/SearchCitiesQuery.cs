using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Outfits.Queries;

public sealed record SearchCitiesQuery(string Query) : IRequest<List<CitySuggestion>>;
