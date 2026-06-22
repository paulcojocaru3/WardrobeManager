using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Outfits.Queries;

public sealed class SearchCitiesQueryHandler(IWeatherService weatherService)
    : IRequestHandler<SearchCitiesQuery, List<CitySuggestion>>
{
    public Task<List<CitySuggestion>> Handle(SearchCitiesQuery request, CancellationToken ct)
    {
        return weatherService.SearchCitiesAsync(request.Query, ct);
    }
}
