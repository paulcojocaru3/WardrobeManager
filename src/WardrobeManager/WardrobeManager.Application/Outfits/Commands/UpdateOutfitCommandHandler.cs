using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Commands;

public class UpdateOutfitCommandHandler(IOutfitRepository outfitRepository, IClothingRepository clothingRepository) : IRequestHandler<UpdateOutfitCommand, bool>
{
    public async Task<bool> Handle(UpdateOutfitCommand request, CancellationToken ct)
    {
        var outfit = await outfitRepository.GetByIdAsync(request.Id, ct);
        if (outfit == null)
        {
            throw new InvalidOperationException($"Outfit with ID {request.Id} was not found.");
        }

        outfit.Name = request.Name;

        var newItems = new List<ClothingItem>();
        foreach (var itemId in request.ItemIds)
        {
            var item = await clothingRepository.GetByIdAsync(itemId, ct);
            if (item != null)
            {
                // Verificăm dacă există deja o piesă de acest tip în lista nouă
                if (newItems.Any(i => i.Type == item.Type))
                {
                    throw new InvalidOperationException($"Outfit already contains an item of type {item.Type}. Each type must be unique.");
                }
                newItems.Add(item);
            }
        }
        
        outfit.Items = newItems;

        await outfitRepository.UpdateAsync(outfit, ct);
        return true;
    }
}
