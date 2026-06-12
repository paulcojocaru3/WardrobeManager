using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Outfits.Commands;

public sealed class ToggleOutfitFavoriteCommandHandler(IOutfitRepository outfitRepository) : IRequestHandler<ToggleOutfitFavoriteCommand, bool>
{
    public async Task<bool> Handle(ToggleOutfitFavoriteCommand request, CancellationToken ct)
    {
        var outfit = await outfitRepository.GetByIdAsync(request.Id, ct);
        if (outfit == null)
        {
            throw new InvalidOperationException($"Outfit with ID {request.Id} was not found.");
        }

        outfit.IsFavorite = !outfit.IsFavorite;

        await outfitRepository.UpdateAsync(outfit, ct);
        return outfit.IsFavorite;
    }
}
