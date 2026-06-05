using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Queries;

namespace WardrobeManager.Application.Outfits.Commands;

public class GenerateOutfitFromPromptCommandHandler(
    IPromptIntentService promptIntentService,
    IOccasionClassifier occasionClassifier,
    IStartItemSelector startItemSelector,
    IOutfitGenerator outfitGenerator,
    IWeatherService weatherService,
    IValidator<GenerateOutfitFromPromptCommand> validator)
    : IRequestHandler<GenerateOutfitFromPromptCommand, GenerateOutfitFromPromptResult>
{
    public async Task<GenerateOutfitFromPromptResult> Handle(GenerateOutfitFromPromptCommand request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        // 1. Understand the prompt (LLM with fallbacks) for the open-ended details
        //    (anchor garment, colors, city).
        var intent = await promptIntentService.ParseAsync(request.Prompt, ct);

        // 1b. Style comes from the deterministic occasion map when a known occasion is
        //     present (reliable + free); the LLM's style is only a fallback.
        var mappedStyle = occasionClassifier.ClassifyStyle(request.Prompt);
        if (mappedStyle != null)
            intent = intent with { Style = mappedStyle };

        // 2. Resolve weather if a city was mentioned.
        WeatherData? weather = null;
        if (!string.IsNullOrWhiteSpace(intent.City))
        {
            try { weather = await weatherService.GetCurrentWeatherAsync(intent.City, ct); }
            catch { weather = null; }
        }

        // 3. Pick the seed item semantically from the user's wardrobe.
        var startItem = await startItemSelector.SelectAsync(request.UserId, intent, ct);
        if (startItem == null)
            throw new InvalidOperationException("No clothing items found to build an outfit from. Add items to your wardrobe first.");

        // 4. Generate the outfit with the enriched context.
        var outfit = await outfitGenerator.GenerateAiOutfitAsync(
            request.UserId, startItem.Id, request.Threshold, weather, intent.Style,
            intent.DesiredColors, intent.AvoidColors, intent.Occasion, ct);

        return new GenerateOutfitFromPromptResult(outfit, intent);
    }
}
