using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Clothing.Commands;

public class DeleteClothingCommandHandler(IClothingRepository clothingRepository) : IRequestHandler<DeleteClothingCommand, bool>
{
    public async Task<bool> Handle(DeleteClothingCommand request, CancellationToken ct)
    {
        var item = await clothingRepository.GetByIdAsync(request.Id, ct);

        if (item == null) return false;

        await clothingRepository.DeleteAsync(item, ct);
        return true;
    }
}