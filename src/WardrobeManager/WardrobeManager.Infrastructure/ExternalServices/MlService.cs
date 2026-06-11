using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Learning;

namespace WardrobeManager.Infrastructure.ExternalServices;

public sealed class MlService(HttpClient httpClient, ILogger<MlService> logger) : IMlService
{
    // Reused across calls — JsonSerializerOptions caches metadata on first use, so a new instance per request defeats it.
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<MlClothingResult> ProcessClothingImageAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);

        var response = await httpClient.PostAsync("process-clothing", form, ct);
        if (!response.IsSuccessStatusCode) throw new Exception("ML API failed.");

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<MlApiResponse>(json, JsonOptions);

        string? type = result?.type;
        if (type == null)
        {
            type = result?.label;
        }

        string color = "unknown";
        if (result?.color != null)
        {
            color = result.color;
        }

        return new MlClothingResult(
            type,
            color,
            result?.processed_image_b64,
            result?.embedding,
            result?.gender,
            result?.season,
            result?.usage);
    }

    public async Task<(string Style, double Confidence, string? City)> ParsePromptAsync(string prompt, CancellationToken ct = default)
    {
        var payload = new { prompt };
        var response = await httpClient.PostAsJsonAsync("parse-prompt", payload, ct);
        if (!response.IsSuccessStatusCode)
            return ("Casual", 0, null);

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<PromptParseResponse>(json, JsonOptions);

        string style = "Casual";
        double confidence = 0;
        string? city = null;
        if (result != null)
        {
            if (result.Style != null)
            {
                style = result.Style;
            }
            confidence = result.StyleConfidence;
            city = result.City;
        }
        return (style, confidence, city);
    }

    public async Task<float[]> EmbedTextAsync(string text, CancellationToken ct = default)
    {
        var payload = new { text };
        var response = await httpClient.PostAsJsonAsync("embed-text", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("embed-text returned {Status} for '{Text}'; seed selection will fall back.", response.StatusCode, text);
            return Array.Empty<float>();
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<EmbedTextResponse>(json, JsonOptions);
        if (result != null && result.embedding is { Length: > 0 })
        {
            return result.embedding;
        }

        logger.LogWarning("embed-text returned an empty embedding for '{Text}'; seed selection will fall back.", text);
        return Array.Empty<float>();
    }

    public async Task<IReadOnlyList<string>> PredictArticleTypesAsync(IReadOnlyList<float[]> embeddings, CancellationToken ct = default)
    {
        if (embeddings.Count == 0) return Array.Empty<string>();

        var payload = new { embeddings };
        var response = await httpClient.PostAsJsonAsync("predict-article-types", payload, ct);
        if (!response.IsSuccessStatusCode) return Array.Empty<string>();

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<PredictArticleTypesResponse>(json, JsonOptions);
        if (result != null && result.types != null)
        {
            return result.types;
        }
        return Array.Empty<string>();
    }

    public async Task<IReadOnlyList<string>> GetArticleTypesAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("article-types", ct);
        if (!response.IsSuccessStatusCode) return Array.Empty<string>();

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<PredictArticleTypesResponse>(json, JsonOptions);
        if (result != null && result.types != null)
        {
            return result.types;
        }
        return Array.Empty<string>();
    }

    public async Task<LearnedWeights?> TrainWeightsAsync(IReadOnlyList<WeightTrainingSample> samples, IReadOnlyList<string> featureNames, IReadOnlyDictionary<string, double> defaultWeights, CancellationToken ct = default)
    {
        var payload = new
        {
            feature_names = featureNames,
            default_weights = defaultWeights,
            samples = samples.Select(s => new { features = s.Features, label = s.Label })
        };

        var response = await httpClient.PostAsJsonAsync("train/weights", payload, ct);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<TrainWeightsResponse>(json, JsonOptions);

        return result?.weights is { Count: > 0 }
            ? new LearnedWeights(result.weights, result.n_samples)
            : null;
    }

    private class TrainWeightsResponse
    {
        public Dictionary<string, double>? weights { get; set; }
        public int n_samples { get; set; }
    }

    private class EmbedTextResponse
    {
        public float[]? embedding { get; set; }
    }

    private class PredictArticleTypesResponse
    {
        public List<string>? types { get; set; }
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