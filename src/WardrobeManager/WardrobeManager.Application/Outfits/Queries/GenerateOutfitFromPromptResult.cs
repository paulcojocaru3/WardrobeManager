using WardrobeManager.Application.Outfits.Prompting;

namespace WardrobeManager.Application.Outfits.Queries;

/// <summary>
/// Result of prompt-driven generation: the suggested outfit plus the parsed intent,
/// so the UI can show the user what the assistant understood.
/// </summary>
public record GenerateOutfitFromPromptResult(AiGeneratedOutfitDto Outfit, PromptIntent Intent);
