using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Commands;

public sealed class CreateOutfitCommandHandler(
    IOutfitRepository outfitRepository, 
    IClothingRepository clothingRepository,
    TimeProvider? clock = null) : IRequestHandler<CreateOutfitCommand, Guid>
{
    public async Task<Guid> Handle(CreateOutfitCommand request, CancellationToken ct)
    {
        var requestedItemIds = request.ItemIds.Distinct().ToList();
        var items = await clothingRepository.GetByIdsForUserAsync(requestedItemIds, request.UserId, ct);
        if (items.Count != requestedItemIds.Count)
        {
            throw new InvalidOperationException("One or more clothing items were not found.");
        }

        var outfit = Outfit.Create(
            request.UserId,
            request.Name,
            items,
            (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime,
            request.IsAiGenerated,
            request.IsEventExclusive,
            request.Tags,
            request.AiGenerationId);

        await outfitRepository.AddAsync(outfit, ct);
        return outfit.Id;
    }
}
