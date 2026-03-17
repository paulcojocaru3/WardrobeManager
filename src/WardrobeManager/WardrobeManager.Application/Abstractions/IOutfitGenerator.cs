using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Abstractions;

public interface IOutfitGenerator
{
    Outfit Generate(User user, ClothingItem startItem, IEnumerable<ClothingItem> availableItems);
}
