using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Infrastructure.ExternalServices;

// ask gemma3 to compose coherent outfits from numbered fashionclip candidates.
public sealed class OllamaOutfitStylist(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<OllamaOutfitStylist> logger) : IOutfitStylist
{
    private const string SystemPrompt = """
        You are a personal wardrobe stylist. Working only from a numbered list of the user's own clothes,
        you assemble complete looks the person could actually wear today and feel put-together in.

        HARD REQUIREMENTS (a look that breaks these is wrong):
        - Hold ONE dressiness level. Read each item's formality tag and keep the whole look on the same rung.
          Do not blend a dressy piece with a sporty or loungey one (tailored trousers must not meet a gym
          tee and slides).
        - Build exactly one TOP, exactly one BOTTOM and exactly one SHOES item. Reach for OUTERWEAR only when
          the context allows it, and add at most one ACCESSORY only if it genuinely lifts the look.
        - Reference only the numbers in the list. Never invent a number and never repeat one.

        TASTE, IN ORDER OF IMPORTANCE:
        1. Colour - anchor on neutrals and let one, maybe two, colours speak; three colours total at most
           (neutrals are free). Shades of a single family always read clean.
        2. Fabric and texture - set a smooth piece against a textured one; avoid two heavy or two glossy
           pieces together, and let fabric weight follow the temperature.
        3. Line and balance - pair a looser piece with a leaner one instead of stacking volume on volume.
        4. Moment - keep daytime fresh and light, evenings deeper and sharper; layer only for cold or rain,
           and make sure the look still stands once the outer layer comes off.

        SIGNALS YOU ARE GIVEN:
        - The structured fields are authoritative. In particular, color= is the real saved colour. Never
          infer or rename an item's colour, material or subtype, and never contradict those fields in prose.
        - The list was cast per slot with FashionCLIP retrieval, then diversified to avoid near-duplicates.
          Lean toward earlier numbers, but make the final styling judgement yourself.
        - "similar alternatives" notes mean those same-slot pieces look alike. Use that to make the three
          looks distinct. Do not treat visual similarity as evidence that two different pieces go together.

        Before returning JSON, silently verify each outfit has top/bottom/shoes, no duplicate single-occupancy
        slots, coherent formality, weather-safe layers, and no obvious color/pattern clash.

        Deliver THREE complete looks, strongest first. If options are limited, correctness beats distinctness.
        Never write "couldn't find", "could not find", "no match", "closest match", "unavailable", or "sorry".
        Headlines, highlights and tips must describe only the numbered items actually selected and must use
        their exact structured attributes. Reply with JSON only, no prose:
        {"outfits":[{"items":[numbers],"headline":"<=5 words","highlights":["one short reason"],"styling_tip":"one specific, non-repetitive tip","checks":{"has_top":true,"has_bottom":true,"has_shoes":true,"formality_consistent":true,"weather_safe":true}}]}
        """;

    public async Task<IReadOnlyList<StylistOutfit>?> ComposeAsync(
        IReadOnlyList<StylistItem> candidates, StylistContext context, CancellationToken ct = default)
    {
        if (candidates.Count == 0) return null;

        var temperature = context.Shuffle ? 0.7 : 0.3;
        var userMessage = BuildUserMessage(candidates, context, repairInstruction: null);
        return await SendAsync(userMessage, temperature, "gemma3 outfit stylist unavailable; keeping the deterministic outfit.", ct);
    }

    public async Task<IReadOnlyList<StylistOutfit>?> RepairAsync(
        IReadOnlyList<StylistItem> candidates,
        StylistContext context,
        IReadOnlyList<StylistOutfit> invalidOutfits,
        string validationError,
        CancellationToken ct = default)
    {
        if (candidates.Count == 0) return null;

        var invalidJson = JsonSerializer.Serialize(new
        {
            outfits = invalidOutfits.Select(o => new
            {
                items = o.ItemNumbers,
                headline = o.Headline,
                highlights = o.Highlights,
                styling_tip = o.StylingTip
            })
        });

        var repairInstruction = $"""
            REPAIR TASK:
            Your previous output was invalid: {validationError}
            Previous output: {invalidJson}

            Fix only the item numbers and return valid JSON. Keep the same intended style and headline when possible.
            Every outfit must include exactly one TOP, one BOTTOM and one SHOES item from the numbered candidates.
            """;

        var userMessage = BuildUserMessage(candidates, context, repairInstruction);
        return await SendAsync(userMessage, 0.3, "gemma3 outfit repair unavailable; keeping the deterministic outfit.", ct);
    }

    private async Task<IReadOnlyList<StylistOutfit>?> SendAsync(
        string userMessage, double temperature, string failureLogMessage, CancellationToken ct)
    {
        try
        {
            var model = configuration["Ollama:Model"] ?? "gemma3";

            var request = new ChatRequest(
                model,
                Stream: false,
                Format: "json",
                Options: new ChatOptions(temperature, 1400),
                Messages: new[]
                {
                    new ChatMessage("system", SystemPrompt),
                    new ChatMessage("user", userMessage),
                });

            var response = await httpClient.PostAsJsonAsync("api/chat", request, ct);
            response.EnsureSuccessStatusCode();

            var chat = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: ct);
            var content = chat?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content)) return null;

            var parsed = JsonSerializer.Deserialize<OutfitsPayload>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return MapOutfits(parsed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, failureLogMessage);
            return null;
        }
    }

    private static string BuildUserMessage(IReadOnlyList<StylistItem> candidates, StylistContext context, string? repairInstruction)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CONTEXT:");
        sb.AppendLine($"- Occasion: {context.Occasion ?? "everyday"}");
        if (!string.IsNullOrWhiteSpace(context.TimeOfDay)) sb.AppendLine($"- Time of day: {context.TimeOfDay}");
        if (!string.IsNullOrWhiteSpace(context.WeatherSummary)) sb.AppendLine($"- Weather: {context.WeatherSummary}");
        if (!string.IsNullOrWhiteSpace(context.Style))
        {
            sb.AppendLine($"- Required style: {context.Style}. Every outfit must read as this style.");
        }
        if (context.FavoriteColors is { Count: > 0 })
        {
            sb.AppendLine($"- Preferred colors: {string.Join(", ", context.FavoriteColors)}. Favor these when the available pieces still form a coherent outfit.");
        }
        if (context.AvoidColors is { Count: > 0 })
        {
            sb.AppendLine($"- Forbidden colors: {string.Join(", ", context.AvoidColors)}. Do not select an item matching these colors.");
        }
        if (!context.AllowOuterwear)
        {
            sb.AppendLine("- Do NOT include any OUTERWEAR. Build top + bottom + shoes (+ optional accessory) only.");
        }
        if (context.MandatoryItemNumber is int mandatory)
        {
            var slot = string.IsNullOrWhiteSpace(context.MandatorySlot) ? "piece" : context.MandatorySlot;
            sb.AppendLine($"- MANDATORY: item [{mandatory}] is the user's chosen {slot}. It MUST be in every outfit. " +
                          $"Never replace it and never pick a different {slot}; build the rest of the look around it.");
        }
        sb.AppendLine();
        sb.AppendLine("SLOT CONTRACT:");
        sb.AppendLine("- TOP: choose exactly 1.");
        sb.AppendLine("- BOTTOM: choose exactly 1.");
        sb.AppendLine("- SHOES: choose exactly 1.");
        sb.AppendLine(context.AllowOuterwear
            ? "- OUTERWEAR: choose 0 or 1 only when useful for the weather."
            : "- OUTERWEAR: choose 0.");
        sb.AppendLine("- ACCESSORY: choose 0 or 1 only if it improves the look.");
        sb.AppendLine();
        sb.AppendLine("FORMALITY SCALE:");
        sb.AppendLine("1 lounge/sport, 2 casual, 3 smart casual, 4 business casual, 5 formal.");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(repairInstruction))
        {
            sb.AppendLine(repairInstruction);
            sb.AppendLine();
        }
        sb.AppendLine("AVAILABLE ITEMS BY SLOT:");
        foreach (var group in candidates.GroupBy(c => c.Slot).OrderBy(g => SlotOrder(g.Key)))
        {
            sb.AppendLine($"{group.Key}:");
            foreach (var c in group) sb.AppendLine($"[{c.Number}] {c.Line}");
            sb.AppendLine();
        }
        sb.AppendLine();
        sb.AppendLine("Compose 3 distinct complete outfits using only these numbers.");
        return sb.ToString();
    }

    private static int SlotOrder(string slot) => slot switch
    {
        "TOP" => 0,
        "BOTTOM" => 1,
        "SHOES" => 2,
        "OUTERWEAR" => 3,
        "ACCESSORY" => 4,
        _ => 99
    };

    private List<StylistOutfit>? MapOutfits(OutfitsPayload? parsed)
    {
        if (parsed?.Outfits is not { Count: > 0 }) return null;

        var result = new List<StylistOutfit>();
        foreach (var o in parsed.Outfits)
        {
            if (o.Items is not { Count: > 0 }) continue;
            var headline = string.IsNullOrWhiteSpace(o.Headline) || ContainsApologeticFallback(o.Headline)
                ? string.Empty
                : o.Headline;
            var highlights = (o.Highlights ?? new List<string>())
                .Where(h => !string.IsNullOrWhiteSpace(h) && !ContainsApologeticFallback(h))
                .ToList();
            var stylingTip = string.IsNullOrWhiteSpace(o.StylingTip) || ContainsApologeticFallback(o.StylingTip)
                ? string.Empty
                : o.StylingTip;
            result.Add(new StylistOutfit(
                o.Items.Distinct().ToList(),
                headline,
                highlights,
                stylingTip));
        }
        return result.Count > 0 ? result : null;
    }

    private static bool ContainsApologeticFallback(string value)
    {
        return value.Contains("couldn't find", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("could not find", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("no match", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("closest match", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("sorry", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ChatRequest(
        string Model, bool Stream, string Format, ChatOptions Options, ChatMessage[] Messages);
    // keep ollama option names aligned with the api contract.
    private sealed record ChatOptions(
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("num_predict")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? NumPredict = null);
    private sealed record ChatMessage(string Role, string Content);
    private sealed record ChatResponse([property: JsonPropertyName("message")] ChatMessage? Message);

    private sealed record OutfitsPayload(
        [property: JsonPropertyName("outfits")] List<OutfitPayload>? Outfits);

    private sealed record OutfitPayload(
        [property: JsonPropertyName("items")] List<int>? Items,
        [property: JsonPropertyName("headline")] string? Headline,
        [property: JsonPropertyName("highlights")] List<string>? Highlights,
        [property: JsonPropertyName("styling_tip")] string? StylingTip);
}
