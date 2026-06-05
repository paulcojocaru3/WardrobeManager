using WardrobeManager.Application.Outfits.Prompting;

namespace WardrobeManager.Application.Abstractions;

/// <summary>
/// Turns a free-text outfit request into structured <see cref="PromptIntent"/>.
/// Implemented by an LLM (Ollama) with graceful fallbacks.
/// </summary>
public interface IPromptIntentService
{
    Task<PromptIntent> ParseAsync(string prompt, CancellationToken ct = default);
}
