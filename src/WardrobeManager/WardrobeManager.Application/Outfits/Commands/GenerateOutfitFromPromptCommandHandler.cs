using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Application.Outfits.Prompting;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Commands;

public sealed class GenerateOutfitFromPromptCommandHandler(
    IPromptIntentService promptIntentService,
    IOccasionClassifier occasionClassifier,
    IGarmentClassifier garmentClassifier,
    IStartItemSelector startItemSelector,
    IOutfitGenerator outfitGenerator,
    IWeatherService weatherService,
    IValidator<GenerateOutfitFromPromptCommand> validator,
    ILogger<GenerateOutfitFromPromptCommandHandler> logger)
    : IRequestHandler<GenerateOutfitFromPromptCommand, GenerateOutfitFromPromptResult>
{
    public async Task<GenerateOutfitFromPromptResult> Handle(GenerateOutfitFromPromptCommand request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var intent = await promptIntentService.ParseAsync(request.Prompt, ct);

        var mappedStyle = occasionClassifier.ClassifyStyle(request.Prompt);
        if (mappedStyle != null)
            intent = intent with { Style = mappedStyle };

        var garments = garmentClassifier.Detect(request.Prompt);
        if (garments.Count > 0)
        {
            var mergedTypes = intent.RequestedTypes
                .Concat(garments.Select(g => g.Type))
                .Distinct()
                .ToList();
            intent = intent with
            {
                RequestedGarments = garments,
                RequestedTypes = mergedTypes,
                AnchorDescription = string.IsNullOrWhiteSpace(intent.AnchorDescription)
                    ? garments[0].SubType
                    : intent.AnchorDescription
            };
        }

        // Per-type slot constraint: deterministic sub-type (last garment wins per type) merged with
        // the LLM's per-garment colors.
        var garmentConstraints = BuildGarmentConstraints(garments, intent.GarmentSpecs);

        // 2. Resolve weather if a city was mentioned.
        WeatherData? weather = null;
        if (!string.IsNullOrWhiteSpace(intent.City))
        {
            try { weather = await weatherService.GetCurrentWeatherAsync(intent.City, ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Weather unavailable for {City}; generating without it.", intent.City);
            }
        }

        // 3. Pick the seed item semantically from the user's wardrobe.
        var startItem = await startItemSelector.SelectAsync(request.UserId, intent, request.ExcludedSeedItemIds, weather, ct);
        if (startItem == null)
            throw new InvalidOperationException("No clothing items found to build an outfit from. Add items to your wardrobe first.");

        // 4. Generate the outfit with the enriched context.
        var outfit = await outfitGenerator.GenerateAiOutfitAsync(
            request.UserId,
            startItem.Id,
            new OutfitGenerationOptions
            {
                Threshold = request.Threshold,
                Weather = weather,
                Style = intent.Style,
                DesiredColors = intent.DesiredColors,
                AvoidColors = intent.AvoidColors,
                Occasion = intent.Occasion,
                Formality = intent.Formality,
                TemperatureHint = intent.TemperatureHint,
                RequestedTypes = intent.RequestedTypes,
                GarmentConstraints = garmentConstraints
            },
            ct);

        return new GenerateOutfitFromPromptResult(outfit, intent);
    }

    private static IReadOnlyDictionary<ClothingType, GarmentSpec> BuildGarmentConstraints(
        IReadOnlyList<RequestedGarment> garments, IReadOnlyList<GarmentSpec> colorSpecs)
    {
        var subTypeByType = garments
            .GroupBy(g => g.Type)
            .ToDictionary(grp => grp.Key, grp => grp.Last().SubType);
        var colorByType = colorSpecs.ToDictionary(s => s.Type);

        var result = new Dictionary<ClothingType, GarmentSpec>();
        foreach (var type in subTypeByType.Keys.Union(colorByType.Keys))
        {
            string? subType = null;
            // Generic words ("top"/"pants"/"shoes") constrain only the type, not the sub-type.
            if (subTypeByType.TryGetValue(type, out var st) && !GarmentVocabulary.IsGenericSubType(st))
                subType = st;

            IReadOnlyList<string> desired = new List<string>();
            IReadOnlyList<string> avoid = new List<string>();
            if (colorByType.TryGetValue(type, out var colors))
            {
                desired = colors.DesiredColors;
                avoid = colors.AvoidColors;
            }

            result[type] = new GarmentSpec { Type = type, SubType = subType, DesiredColors = desired, AvoidColors = avoid };
        }
        return result;
    }
}
