using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Prompting;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Infrastructure.ExternalServices;

/// <summary>
/// Parses outfit prompts into <see cref="PromptIntent"/> using a local Ollama LLM.
/// Three layers for reliable results: (1) a JSON Schema constrains the model's output
/// to valid types/enums at decode time, (2) <see cref="Coerce"/> defensively normalizes
/// the result, (3) a semantics-focused prompt with a few-shot example. Falls back to the
/// Python ML parser (and finally a safe default) when Ollama is unavailable.
/// </summary>
public class OllamaPromptIntentService : IPromptIntentService
{
    private readonly HttpClient _httpClient;
    private readonly IMlService _mlService;
    private readonly string _model;

    private static readonly string[] AllowedStyles =
        { "Casual", "Ethnic", "Formal", "Party", "Smart Casual", "Sports", "Travel" };

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // Layer 1: JSON Schema handed to Ollama's `format` so generation is constrained to
    // valid types/enums at decode time (eliminates string-where-int and invalid enum
    // values such as "Tops"). The schema enforces shape; Coerce() handles the rest.
    private static readonly object ResponseSchema = new
    {
        type = "object",
        properties = new
        {
            style = new { type = new[] { "string", "null" }, @enum = new object?[] { "Casual", "Ethnic", "Formal", "Party", "Smart Casual", "Sports", "Travel", null } },
            city = new { type = new[] { "string", "null" } },
            occasion = new { type = new[] { "string", "null" } },
            desiredColors = new { type = "array", items = new { type = "string" } },
            avoidColors = new { type = "array", items = new { type = "string" } },
            anchorDescription = new { type = new[] { "string", "null" } },
            requestedTypes = new { type = "array", items = new { type = "string", @enum = new[] { "Top", "Bottom", "Shoes", "Outerwear", "Accessory" } } },
            formality = new { type = new[] { "integer", "null" } },
            temperatureHint = new { type = new[] { "string", "null" }, @enum = new object?[] { "cold", "mild", "warm", "hot", null } }
        },
        required = new[] { "style", "city", "occasion", "desiredColors", "avoidColors", "anchorDescription", "requestedTypes", "formality", "temperatureHint" }
    };

    // Layer 3: prompt focuses on SEMANTICS (the schema already guarantees shape).
    // Includes the noun-phrase rule for anchorDescription (better embeddings) and a
    // Romanian few-shot example.
    private const string SystemPrompt = """
        You are a fashion assistant that extracts structured outfit intent from a user's request.
        Guidelines:
        - LANGUAGE: ALWAYS output anchorDescription and every color in English, lowercase, even when the user
          writes in Romanian. Translate garments: camasa->shirt, tricou->t-shirt, maieu->tank top, blugi->jeans,
          pantaloni->pants, rochie->dress, fusta->skirt, geaca->jacket, palton->coat, pantofi->shoes.
        - style: map the activity/occasion to the closest style, recognizing Romanian words too.
          Sports: gym, running, jogging, workout, alergat, alergare, sala, antrenament, fitness, sport, drumetie, munte, schi, tenis, fotbal.
          Formal: wedding, ceremony, gala, nunta, ceremonie.
          Smart Casual: office, meeting, interview, birou, interviu, intalnire de afaceri.
          Party: club, party, birthday, petrecere, aniversare.
          Travel: travel, flight, vacation, vacanta, zbor, calatorie.
          Casual: everyday, errands, walk, plimbare, zi cu zi.
        - COLORS: include a color in desiredColors or avoidColors ONLY if the user explicitly writes that color
          word. If the user mentions no colors, BOTH arrays MUST be empty []. Never guess, infer, or invent colors.
        - anchorDescription: fill ONLY if the user names a specific garment they want to wear, as a SHORT English
          noun phrase that INCLUDES the color when the user gave one (e.g. "white shirt"); otherwise null.
        - requestedTypes: clothing types the user explicitly asks for (a shirt/t-shirt/tank is "Top"); otherwise [].
        - city: a city mentioned for weather, otherwise null.
        - formality: 1 (very casual) to 5 (very formal), or null.
        Use null or [] when the text does not support a field.

        Example 1:
        User: "ceva elegant negru pentru o nunta in Bucuresti, fara verde"
        Output: {"style":"Formal","city":"Bucuresti","occasion":"wedding","desiredColors":["black"],"avoidColors":["green"],"anchorDescription":null,"requestedTypes":[],"formality":5,"temperatureHint":null}

        Example 2:
        User: "vreau un outfit pentru o nunta"
        Output: {"style":"Formal","city":null,"occasion":"wedding","desiredColors":[],"avoidColors":[],"anchorDescription":null,"requestedTypes":[],"formality":5,"temperatureHint":null}

        Example 3:
        User: "vreau un outfit cu o camasa alba"
        Output: {"style":null,"city":null,"occasion":null,"desiredColors":["white"],"avoidColors":[],"anchorDescription":"white shirt","requestedTypes":["Top"],"formality":null,"temperatureHint":null}

        Example 4:
        User: "ceva de alergat"
        Output: {"style":"Sports","city":null,"occasion":"running","desiredColors":[],"avoidColors":[],"anchorDescription":null,"requestedTypes":[],"formality":1,"temperatureHint":null}
        """;

    public OllamaPromptIntentService(HttpClient httpClient, IConfiguration configuration, IMlService mlService)
    {
        _httpClient = httpClient;
        _mlService = mlService;
        _model = configuration["Ollama:Model"] ?? "llama3.2";
    }

    public async Task<PromptIntent> ParseAsync(string prompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return new PromptIntent { Style = "Casual" };

        try
        {
            var requestBody = new
            {
                model = _model,
                stream = false,
                format = ResponseSchema,
                options = new { temperature = 0.1 },
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = prompt }
                }
            };

            var response = await _httpClient.PostAsJsonAsync("api/chat", requestBody, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var chat = JsonSerializer.Deserialize<OllamaChatResponse>(json, JsonOptions);
            var content = chat?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
                return await FallbackAsync(prompt, ct);

            var dto = JsonSerializer.Deserialize<IntentDto>(content, JsonOptions);
            return dto == null ? await FallbackAsync(prompt, ct) : Coerce(dto);
        }
        catch
        {
            return await FallbackAsync(prompt, ct);
        }
    }

    private async Task<PromptIntent> FallbackAsync(string prompt, CancellationToken ct)
    {
        try
        {
            var (style, _, city) = await _mlService.ParsePromptAsync(prompt, ct);
            return new PromptIntent { Style = NormalizeStyle(style), City = string.IsNullOrWhiteSpace(city) ? null : city };
        }
        catch
        {
            return new PromptIntent { Style = "Casual" };
        }
    }

    private static PromptIntent Coerce(IntentDto dto)
    {
        var requestedTypes = new List<ClothingType>();
        if (dto.RequestedTypes != null)
        {
            foreach (var t in dto.RequestedTypes)
            {
                if (Enum.TryParse<ClothingType>(t, ignoreCase: true, out var parsed))
                    requestedTypes.Add(parsed);
            }
        }

        return new PromptIntent
        {
            Style = NormalizeStyle(dto.Style),
            City = Clean(dto.City),
            Occasion = Clean(dto.Occasion),
            DesiredColors = NormalizeColors(dto.DesiredColors),
            AvoidColors = NormalizeColors(dto.AvoidColors),
            AnchorDescription = Clean(dto.AnchorDescription),
            RequestedTypes = requestedTypes.Distinct().ToList(), // Layer 2: dedupe (model may repeat, e.g. ["Shoes","Shoes"])
            Formality = dto.Formality is >= 1 and <= 5 ? dto.Formality : null,
            TemperatureHint = Clean(dto.TemperatureHint)?.ToLowerInvariant()
        };
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeStyle(string? style)
    {
        if (string.IsNullOrWhiteSpace(style)) return null;
        return AllowedStyles.FirstOrDefault(s => s.Equals(style.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> NormalizeColors(IEnumerable<string>? colors)
    {
        if (colors == null) return Array.Empty<string>();
        return colors
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    private class OllamaChatResponse
    {
        public OllamaMessage? Message { get; set; }
    }

    private class OllamaMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    private class IntentDto
    {
        public string? Style { get; set; }
        public string? City { get; set; }
        public string? Occasion { get; set; }
        public List<string>? DesiredColors { get; set; }
        public List<string>? AvoidColors { get; set; }
        public string? AnchorDescription { get; set; }
        public List<string>? RequestedTypes { get; set; }
        public int? Formality { get; set; }
        public string? TemperatureHint { get; set; }
    }
}
