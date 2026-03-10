using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Clothing.Queries;

public class GetClothingItemsQueryHandler(IClothingRepository clothingRepository) : IRequestHandler<GetClothingItemsQuery, List<ClothingItem>>
{
    public async Task<List<ClothingItem>> Handle(GetClothingItemsQuery request, CancellationToken ct)
    {
        // Nu mai folosim .ToListAsync() aici pentru ca este o metoda EF Core.
        // In loc de asta, luam Query-ul si il convertim in lista.
        // Ideal, Repository-ul ar trebui sa ne dea direct rezultatul final.
        
        return clothingRepository.Query()
            .Where(i => i.UserId == request.UserId)
            .OrderByDescending(i => i.CreatedAt)
            .ToList(); // ToList() este standard LINQ, nu are nevoie de EF Core.
    }
}