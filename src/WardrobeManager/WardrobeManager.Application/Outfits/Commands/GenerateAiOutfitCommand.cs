using MediatR;
using WardrobeManager.Application.Outfits.Generation;

namespace WardrobeManager.Application.Outfits.Commands;

public record GenerateAiOutfitCommand(
    Guid UserId,
    Guid? StartItemId = null,
    double Threshold = 0.5,
    string? City = null,
    string? Style = null,
    bool PreferUnusedItems = false,
    bool AnchorOnUnused = false,
    string? Occasion = null,
    bool Shuffle = false) : IRequest<AiGeneratedOutfitDto>;
