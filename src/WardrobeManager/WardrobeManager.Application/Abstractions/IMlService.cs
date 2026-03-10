using Microsoft.AspNetCore.Http;

namespace WardrobeManager.Application.Abstractions;

public interface IMlService
{
    Task<(string? Type, string? Color, string? ProcessedImageB64)> ProcessClothingImageAsync(IFormFile file, CancellationToken ct = default);
}