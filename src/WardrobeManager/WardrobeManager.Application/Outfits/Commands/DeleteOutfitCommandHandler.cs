using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Outfits.Commands;

public class DeleteOutfitCommandHandler(IOutfitRepository outfitRepository) : IRequestHandler<DeleteOutfitCommand, bool>
{
    public async Task<bool> Handle(DeleteOutfitCommand request, CancellationToken ct)
    {
        var outfit = await outfitRepository.GetByIdAsync(request.Id, ct);
        if (outfit == null)
        {
            throw new InvalidOperationException($"Outfit with ID {request.Id} was not found.");
        }

        await outfitRepository.DeleteAsync(outfit, ct);
        return true;
    }
}
