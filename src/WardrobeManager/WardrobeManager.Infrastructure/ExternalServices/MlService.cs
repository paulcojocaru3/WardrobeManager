using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Infrastructure.ExternalServices;

public class MlService(HttpClient httpClient) : IMlService
{
    public async Task<(string? Type, string? Color, string? ProcessedImageB64, float[]? Embedding, string? Gender, string? Season, string? Usage)> ProcessClothingImageAsync(IFormFile file, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        using var fileStream = file.OpenReadStream();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        var response = await httpClient.PostAsync("process-clothing", content, ct);
        if (!response.IsSuccessStatusCode) throw new Exception("ML API failed.");

        var json = await response.Content.ReadAsStringAsync(ct);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<MlApiResponse>(json, options);
        string? finalType = result?.type ?? result?.label;
        string? finalColor = result?.color ?? "unknown";

        return (finalType, finalColor, result?.processed_image_b64, result?.embedding, result?.gender, result?.season, result?.usage);
    }

    public async Task<(string Style, double Confidence, string? City)> ParsePromptAsync(string prompt, CancellationToken ct = default)
    {
        var payload = new { prompt };
        var response = await httpClient.PostAsJsonAsync("parse-prompt", payload, ct);
        if (!response.IsSuccessStatusCode)
            return ("Casual", 0, null);

        var json = await response.Content.ReadAsStringAsync(ct);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<PromptParseResponse>(json, options);
        return (result?.Style ?? "Casual", result?.StyleConfidence ?? 0, result?.City);
    }

    public async Task<float[]> EmbedTextAsync(string text, CancellationToken ct = default)
    {
        var payload = new { text };
        var response = await httpClient.PostAsJsonAsync("embed-text", payload, ct);
        if (!response.IsSuccessStatusCode) return Array.Empty<float>();

        var json = await response.Content.ReadAsStringAsync(ct);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<EmbedTextResponse>(json, options);
        return result?.embedding ?? Array.Empty<float>();
    }

    private class EmbedTextResponse
    {
        public float[]? embedding { get; set; }
    }

    private class MlApiResponse
    {
        public string? type { get; set; }
        public string? color { get; set; }
        public string? label { get; set; }
        public string? gender { get; set; }
        public string? season { get; set; }
        public string? usage { get; set; }
        public string? processed_image_b64 { get; set; }
        public float[]? embedding { get; set; }
    }

    private class PromptParseResponse
    {
        public string? Style { get; set; }
        public double StyleConfidence { get; set; }
        public string? City { get; set; }
    }
}