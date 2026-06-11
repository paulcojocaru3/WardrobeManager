using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Generation;

namespace WardrobeManager.Application.Outfits.Commands;

public sealed class GenerateAiOutfitCommandHandler(
    IOutfitGenerator outfitGenerator,
    IWeatherService weatherService,
    IValidator<GenerateAiOutfitCommand> validator) : IRequestHandler<GenerateAiOutfitCommand, AiGeneratedOutfitDto>
{
    public async Task<AiGeneratedOutfitDto> Handle(GenerateAiOutfitCommand request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        WeatherData? weather = null;
        if (!string.IsNullOrEmpty(request.City))
        {
            weather = await weatherService.GetCurrentWeatherAsync(request.City, ct);
        }

        return await outfitGenerator.GenerateAiOutfitAsync(
            request.UserId,
            request.StartItemId,
            new OutfitGenerationOptions
            {
                Threshold = request.Threshold,
                Weather = weather,
                Style = request.Style
            },
            ct);
    }
}
