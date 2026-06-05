using WardrobeManager.Domain.Entities;
using WardrobeManager.Application.Outfits.Queries;

namespace WardrobeManager.Application.Abstractions;

public interface IOutfitGenerator
{
    Task<AiGeneratedOutfitDto> GenerateAiOutfitAsync(Guid userId, Guid startItemId, double threshold = 0.5, WeatherData? weatherData = null, string? style = null, IReadOnlyList<string>? desiredColors = null, IReadOnlyList<string>? avoidColors = null, string? occasion = null, CancellationToken ct = default);
}
