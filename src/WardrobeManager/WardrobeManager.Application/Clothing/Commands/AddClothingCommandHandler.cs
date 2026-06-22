using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Clothing.Commands;

public sealed class AddClothingCommandHandler(
    IClothingRepository clothingRepository,
    IUserRepository userRepository,
    TimeProvider? clock = null)
    : IRequestHandler<AddClothingCommand, ClothingItem>
{
    public async Task<ClothingItem> Handle(AddClothingCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {request.UserId} was not found.");
        }

        var newItem = ClothingItem.Create(
            request.UserId,
            request.Name,
            request.Type,
            request.SubType,
            request.Color,
            request.Gender,
            request.Season,
            request.Usage,
            request.ProcessedImageB64,
            request.Embedding,
            (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime);

        await clothingRepository.AddAsync(newItem, ct);

        return newItem;
    }
}
