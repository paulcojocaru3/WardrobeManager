using Microsoft.AspNetCore.Http;

namespace WardrobeManager.Application.Abstractions;

public interface IMlService
{
    Task<(string? Type, string? Color, string? ProcessedImageB64, float[]? Embedding, string? Gender, string? Season, string? Usage)> ProcessClothingImageAsync(IFormFile file, CancellationToken ct = default);
}