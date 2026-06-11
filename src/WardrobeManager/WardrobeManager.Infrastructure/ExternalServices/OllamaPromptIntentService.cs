using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Application.Outfits.Prompting;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Infrastructure.ExternalServices;
public sealed class OllamaPromptIntentService : IPromptIntentService
{
    private readonly HttpClient _httpClient;
    private readonly IMlService _mlService;
    private readonly string _model;

    private static readonly string[] AllowedStyles =
        { "Casual", "Ethnic", "Formal", "Party", "Smart Casual", "Sports", "Travel" };

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    
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
            garments = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        type = new { type = "string", @enum = new[] { "Top", "Bottom", "Shoes", "Outerwear", "Accessory" } },
                        desiredColors = new { type = "array", items = new { type = "string" } },
                        avoidColors = new { type = "array", items = new { type = "string" } }
                    },
                    required = new[] { "type", "desiredColors", "avoidColors" }
                }
            },
            formality = new { type = new[] { "integer", "null" } },
            temperatureHint = new { type = new[] { "string", "null" }, @enum = new object?[] { "cold", "mild", "warm", "hot", null } }
        },
        required = new[] { "style", "city", "occasion", "desiredColors", "avoidColors", "anchorDescription", "requestedTypes", "garments", "formality", "temperatureHint" }
    };
    
    private const string SystemPrompt = """
        You are a fashion assistant that extracts structured outfit intent from a user's request.
        Guidelines:
        - LANGUAGE: ALWAYS output anchorDescription and every color in English, lowercase, even when the user
          writes in Romanian. Translate garments: camasa->shirt, tricou->t-shirt, maieu->tank top, blugi->jeans,
          pantaloni->pants, rochie->dress, fusta->skirt, geaca->jacket, palton->coat, pantofi->shoes.
          Translate colors to their BASE word, never a shade: alb->white, negru->black, albastru/albastri->blue,
          rosu/rosie->red, verde->green, galben->yellow, gri->gray, maro->brown, roz->pink, mov->purple,
          portocaliu->orange, bej->beige. (e.g. "albastri" -> "blue", NOT "light blue".)
        - style: MUST be exactly one of: Casual, Ethnic, Formal, Party, Smart Casual, Sports, Travel. Map carefully:
          A direct style word the user types maps to that style: casual/lejer/comod -> Casual,
          formal/elegant -> Formal, smart casual/business casual -> Smart Casual, sport/sportiv/sporty -> Sports,
          party -> Party, travel -> Travel, traditional/port/ie -> Ethnic.
          Sports: gym, running, jogging, workout, alergat, alergare, sala, antrenament, fitness, sport, sportiv, sporty, drumetie.
          Formal: wedding, ceremony, gala, nunta, ceremonie, elegant, formal.
          Smart Casual: office, meeting, interview, birou, interviu, intalnire de afaceri, date, smart casual, business casual.
          Party: club, party, birthday, petrecere, aniversare, festival.
          Travel: travel, flight, vacation, vacanta, zbor, calatorie.
          Ethnic: traditional, port, ie.
          Casual: everyday, errands, walk, plimbare, zi cu zi, relaxare, casual, lejer, comod.
        - COLORS: include a color ONLY if the user explicitly writes that color word. Never guess, infer or invent.
          Decide WHERE each color goes:
            * A color tied to a NAMED garment ("black pants", "a tee that is NOT black", "pantaloni negri")
              goes in `garments` under that garment's clothing type — NEVER in the global desiredColors/avoidColors.
            * A color for the whole outfit with NO specific garment ("an all-black outfit", "ceva negru") goes in
              the global desiredColors/avoidColors.
          If the user mentions no colors at all, desiredColors, avoidColors and garments MUST all be [].
        - garments: one entry per garment that has a bound color: {"type": <clothing type>, "desiredColors": [...],
          "avoidColors": [...]}. A color stated POSITIVELY for a garment ("blugi albastri" = blue jeans, "camasa
          alba" = white shirt) goes in that garment's desiredColors. Only an explicit NEGATION ("care nu este alb",
          "fara negru", "sa nu fie negru") goes in avoidColors, and ONLY for the garment that negation describes —
          never flip a wanted color into avoidColors, and never move a negation onto a different garment.
          Map each garment to its type: shirt/t-shirt/tee/tank/top/blouse/dress=Top;
          pants/trousers/jeans/shorts/skirt/leggings=Bottom; shoes/sneakers/trainers/boots/sandals/heels/loafers=Shoes;
          jacket/coat/blazer/hoodie/sweater/cardigan=Outerwear; hat/cap/scarf/belt/bag/watch/sunglasses/tie=Accessory.
          Add a separate entry for EVERY garment the user gives a color, including shoes, outerwear and accessories.
          If no garment has a bound color, garments MUST be [].
        - anchorDescription: fill ONLY if the user names a specific garment they want to wear, as a SHORT English
          noun phrase that INCLUDES the color when the user gave a desired one (e.g. "white shirt"); otherwise null.
        - requestedTypes: clothing types the user explicitly asks for (a shirt/t-shirt/tank is "Top"); otherwise [].
        - city: a city mentioned for weather, otherwise null.
        - formality: 1 (very casual) to 5 (very formal), or null.
        Use null or [] when the text does not support a field.

        Example 1:
        User: "ceva elegant negru pentru o nunta in Bucuresti, fara verde"
        Output: {"style":"Formal","city":"Bucuresti","occasion":"wedding","desiredColors":["black"],"avoidColors":["green"],"anchorDescription":null,"requestedTypes":[],"garments":[],"formality":5,"temperatureHint":null}

        Example 2:
        User: "vreau un outfit cu o camasa alba"
        Output: {"style":null,"city":null,"occasion":null,"desiredColors":[],"avoidColors":[],"anchorDescription":"white shirt","requestedTypes":["Top"],"garments":[{"type":"Top","desiredColors":["white"],"avoidColors":[]}],"formality":null,"temperatureHint":null}

        Example 3:
        User: "ceva de alergat la munte"
        Output: {"style":"Sports","city":null,"occasion":"running","desiredColors":[],"avoidColors":[],"anchorDescription":null,"requestedTypes":[],"garments":[],"formality":1,"temperatureHint":null}

        Example 4:
        User: "un tricou care sa nu fie negru sau alb, si pantaloni negri"
        Output: {"style":null,"city":null,"occasion":null,"desiredColors":[],"avoidColors":[],"anchorDescription":"t-shirt","requestedTypes":["Top","Bottom"],"garments":[{"type":"Top","desiredColors":[],"avoidColors":["black","white"]},{"type":"Bottom","desiredColors":["black"],"avoidColors":[]}],"formality":null,"temperatureHint":null}

        Example 5:
        User: "creeaza outfit cu blugi albastri si o camasa alba"
        Output: {"style":null,"city":null,"occasion":null,"desiredColors":[],"avoidColors":[],"anchorDescription":"blue jeans","requestedTypes":["Bottom","Top"],"garments":[{"type":"Bottom","desiredColors":["blue"],"avoidColors":[]},{"type":"Top","desiredColors":["white"],"avoidColors":[]}],"formality":null,"temperatureHint":null}

        Example 6:
        User: "vreau un outfit cu blugi albastri si un tricou care nu este alb sau negru"
        Output: {"style":null,"city":null,"occasion":null,"desiredColors":[],"avoidColors":[],"anchorDescription":"blue jeans","requestedTypes":["Bottom","Top"],"garments":[{"type":"Bottom","desiredColors":["blue"],"avoidColors":[]},{"type":"Top","desiredColors":[],"avoidColors":["white","black"]}],"formality":null,"temperatureHint":null}

        Example 7:
        User: "create an outfit with blue jeans, a t-shirt that is not white or black and a pair of white shoes"
        Output: {"style":null,"city":null,"occasion":null,"desiredColors":[],"avoidColors":[],"anchorDescription":"blue jeans","requestedTypes":["Bottom","Top","Shoes"],"garments":[{"type":"Bottom","desiredColors":["blue"],"avoidColors":[]},{"type":"Top","desiredColors":[],"avoidColors":["white","black"]},{"type":"Shoes","desiredColors":["white"],"avoidColors":[]}],"formality":null,"temperatureHint":null}

        Example 8:
        User: "black pants with a beige jacket"
        Output: {"style":null,"city":null,"occasion":null,"desiredColors":[],"avoidColors":[],"anchorDescription":"black pants","requestedTypes":["Bottom","Outerwear"],"garments":[{"type":"Bottom","desiredColors":["black"],"avoidColors":[]},{"type":"Outerwear","desiredColors":["beige"],"avoidColors":[]}],"formality":null,"temperatureHint":null}

        Example 9:
        User: "a white shirt, brown shoes and a black belt"
        Output: {"style":null,"city":null,"occasion":null,"desiredColors":[],"avoidColors":[],"anchorDescription":"white shirt","requestedTypes":["Top","Shoes","Accessory"],"garments":[{"type":"Top","desiredColors":["white"],"avoidColors":[]},{"type":"Shoes","desiredColors":["brown"],"avoidColors":[]},{"type":"Accessory","desiredColors":["black"],"avoidColors":[]}],"formality":null,"temperatureHint":null}
        """;

    public OllamaPromptIntentService(HttpClient httpClient, IConfiguration configuration, IMlService mlService)
    {
        _httpClient = httpClient;
        _mlService = mlService;
        var model = configuration["Ollama:Model"];
        if (model == null)
        {
            model = "llama3.2";
        }
        _model = model;
    }

    public async Task<PromptIntent> ParseAsync(string prompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return new PromptIntent { Style = null };

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
            return new PromptIntent { Style = null };
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

        var garmentSpecs = CoerceGarments(dto.Garments);

        // A color bound to a garment lives only on that slot; drop it from the global lists so the
        // outfit-wide evaluator doesn't veto it everywhere (e.g. "black pants" must not avoid black globally).
        var assigned = garmentSpecs
            .SelectMany(g => g.DesiredColors.Concat(g.AvoidColors))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var desiredColors = NormalizeColors(dto.DesiredColors).Where(c => !assigned.Contains(c)).ToList();
        var avoidColors = NormalizeColors(dto.AvoidColors).Where(c => !assigned.Contains(c)).ToList();

        return new PromptIntent
        {
            Style = NormalizeStyle(dto.Style),
            City = Clean(dto.City),
            Occasion = Clean(dto.Occasion),
            DesiredColors = desiredColors,
            AvoidColors = avoidColors,
            GarmentSpecs = garmentSpecs,
            AnchorDescription = Clean(dto.AnchorDescription),
            RequestedTypes = requestedTypes.Distinct().ToList(), // Layer 2: dedupe (model may repeat, e.g. ["Shoes","Shoes"])
            Formality = dto.Formality is >= 1 and <= 5 ? dto.Formality : null,
            TemperatureHint = Clean(dto.TemperatureHint)?.ToLowerInvariant()
        };
    }

    // One GarmentSpec per clothing type (merging colors if the model repeats a type). SubType is
    // left null here — the deterministic garment classifier fills it in the handler.
    private static IReadOnlyList<GarmentSpec> CoerceGarments(List<GarmentDto>? garments)
    {
        if (garments == null) return new List<GarmentSpec>();

        var byType = new Dictionary<ClothingType, (List<string> Desired, List<string> Avoid)>();
        foreach (var g in garments)
        {
            if (g?.Type == null) continue;
            if (!Enum.TryParse<ClothingType>(g.Type, ignoreCase: true, out var type)) continue;

            if (!byType.TryGetValue(type, out var entry))
            {
                entry = (new List<string>(), new List<string>());
                byType[type] = entry;
            }
            entry.Desired.AddRange(NormalizeColors(g.DesiredColors));
            entry.Avoid.AddRange(NormalizeColors(g.AvoidColors));
        }

        var result = new List<GarmentSpec>();
        foreach (var (type, entry) in byType)
        {
            var desired = entry.Desired.Distinct().ToList();
            var avoid = entry.Avoid.Distinct().ToList();
            if (desired.Count == 0 && avoid.Count == 0) continue; // an entry the model emitted with no colors
            result.Add(new GarmentSpec { Type = type, DesiredColors = desired, AvoidColors = avoid });
        }
        return result;
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
        public List<GarmentDto>? Garments { get; set; }
        public int? Formality { get; set; }
        public string? TemperatureHint { get; set; }
    }

    private class GarmentDto
    {
        public string? Type { get; set; }
        public List<string>? DesiredColors { get; set; }
        public List<string>? AvoidColors { get; set; }
    }
}
