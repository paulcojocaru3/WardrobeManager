using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Outfits.Commands;

public sealed class DeleteOutfitCommandHandler(
    IOutfitRepository outfitRepository) : IRequestHandler<DeleteOutfitCommand, bool>
{
    public async Task<bool> Handle(DeleteOutfitCommand request, CancellationToken ct)
    {
        var outfit = await outfitRepository.GetByIdForUserAsync(request.Id, request.UserId, ct);
        if (outfit == null)
        {
            return false;
        }

        await outfitRepository.DeleteAsync(outfit, ct);
        return true;
    }
}
