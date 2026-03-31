using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Clothing.Commands;

public class AddClothingCommandHandler(
    IClothingRepository clothingRepository, 
    IUserRepository userRepository,
    IValidator<AddClothingCommand> validator) 
    : IRequestHandler<AddClothingCommand, ClothingItem>
{
    public async Task<ClothingItem> Handle(AddClothingCommand request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {request.UserId} was not found.");
        }

        var newItem = new ClothingItem
        {
            UserId = request.UserId,
            Name = request.Name,
            Type = request.Type,
            Color = request.Color, 
            Gender = request.Gender,
            Season = request.Season,
            Usage = request.Usage,
            ProcessedImageUrl = request.ProcessedImageB64.StartsWith("data:image") 
                ? request.ProcessedImageB64 
                : $"data:image/png;base64,{request.ProcessedImageB64}",
            OriginalImageUrl = "saved_locally",
            Embedding = request.Embedding,
            CreatedAt = DateTime.UtcNow
        };

        await clothingRepository.AddAsync(newItem, ct);
        return newItem;
    }
}
