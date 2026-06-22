using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Clothing.Commands;

public sealed class DeleteClothingCommandHandler(IClothingRepository clothingRepository) : IRequestHandler<DeleteClothingCommand, bool>
{
    public async Task<bool> Handle(DeleteClothingCommand request, CancellationToken ct)
    {
        var item = await clothingRepository.GetByIdForUserAsync(request.Id, request.UserId, ct);
        if (item == null)
        {
            return false;
        }

        await clothingRepository.DeleteAsync(item, ct);
        return true;
    }
}
