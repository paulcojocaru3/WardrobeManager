using Microsoft.AspNetCore.Http;

namespace WardrobeManager.Application.Abstractions;

public interface IMlService
{
    Task<(string? Type, string? Color, string? ProcessedImageB64, float[]? Embedding, string? Gender, string? Season, string? Usage)> ProcessClothingImageAsync(IFormFile file, CancellationToken ct = default);
    Task<(string Style, double Confidence, string? City)> ParsePromptAsync(string prompt, CancellationToken ct = default);

    /// <summary>Returns the Fashion-CLIP text embedding for an arbitrary phrase (same space as image embeddings).</summary>
    Task<float[]> EmbedTextAsync(string text, CancellationToken ct = default);
}