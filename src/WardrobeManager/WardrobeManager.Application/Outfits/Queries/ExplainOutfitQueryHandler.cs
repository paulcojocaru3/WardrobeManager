using MediatR;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Explaining;
using WardrobeManager.Application.Outfits.Scoring;

namespace WardrobeManager.Application.Outfits.Queries;

public sealed class ExplainOutfitQueryHandler(
    IClothingRepository clothingRepository,
    IWeatherService weatherService,
    IEnumerable<IOutfitEvaluator> evaluators,
    IStylingNotesService stylingNotesService,
    ILogger<ExplainOutfitQueryHandler> logger)
    : IRequestHandler<ExplainOutfitQuery, StylingNotesResult>
{
    public async Task<StylingNotesResult> Handle(ExplainOutfitQuery request, CancellationToken ct)
    {
        var explanation = await OutfitExplanationFactory.BuildAsync(
            clothingRepository, weatherService, evaluators, logger,
            request.ItemIds, request.Style, request.Occasion, request.City, request.Tradeoffs, ct);

        if (explanation.Pieces.Count == 0) return new StylingNotesResult(Array.Empty<string>());

        var notes = await stylingNotesService.GenerateAsync(explanation, ct);
        return new StylingNotesResult(notes);
    }
}
