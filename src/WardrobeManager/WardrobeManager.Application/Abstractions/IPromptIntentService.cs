using WardrobeManager.Application.Outfits.Prompting;

namespace WardrobeManager.Application.Abstractions;

// parses a free-text outfit request into a PromptIntent (Ollama LLM, with fallbacks)
public interface IPromptIntentService
{
    Task<PromptIntent> ParseAsync(string prompt, CancellationToken ct = default);
}
