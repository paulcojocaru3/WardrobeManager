using MediatR;
using WardrobeManager.Application.Outfits.Queries;

namespace WardrobeManager.Application.Outfits.Commands;

public record GenerateOutfitFromPromptCommand(
    Guid UserId,
    string Prompt,
    double Threshold = 0.5,
    IReadOnlyList<Guid>? ExcludedSeedItemIds = null)
    : IRequest<GenerateOutfitFromPromptResult>;
