using MediatR;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Explaining;
using WardrobeManager.Application.Outfits.Scoring;

namespace WardrobeManager.Application.Outfits.Queries;

public sealed class OutfitInsightQueryHandler(
    IClothingRepository clothingRepository,
    IWeatherService weatherService,
    IEnumerable<IOutfitEvaluator> evaluators,
    IStylingNotesService stylingNotesService,
    ILogger<OutfitInsightQueryHandler> logger)
    : IRequestHandler<OutfitInsightQuery, OutfitInsight>
{
    public async Task<OutfitInsight> Handle(OutfitInsightQuery request, CancellationToken ct)
    {
        var explanation = await OutfitExplanationFactory.BuildAsync(
            clothingRepository, weatherService, evaluators, logger,
            request.ItemIds, request.Style, request.Occasion, request.City, request.Tradeoffs, ct);

        if (explanation.Pieces.Count == 0) return new OutfitInsight();

        return await stylingNotesService.GenerateInsightAsync(explanation, ct);
    }
}
