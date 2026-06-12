using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Application.Outfits.Prompting;

namespace WardrobeManager.Application.Outfits.Queries;

// the suggested outfit + the parsed intent, so the UI can show what the assistant understood
public record GenerateOutfitFromPromptResult(AiGeneratedOutfitDto Outfit, PromptIntent Intent);
