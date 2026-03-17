using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Clothing.Commands;

public class UploadClothingCommandHandler(
    IClothingRepository clothingRepository, 
    IUserRepository userRepository,
    IMlService mlService, 
    IValidator<UploadClothingCommand> validator) 
    : IRequestHandler<UploadClothingCommand, ClothingItem>
{
    public async Task<ClothingItem> Handle(UploadClothingCommand request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {request.UserId} was not found.");
        }

        var (typeLabel, colorLabel, processedImageB64) = await mlService.ProcessClothingImageAsync(request.File, ct);

        var newItem = new ClothingItem
        {
            UserId = request.UserId,
            Name = request.Name, // Numele setat de utilizator
            Type = MapToClothingType(typeLabel),
            Color = colorLabel, 
            ProcessedImageUrl = $"data:image/png;base64,{processedImageB64}",
            OriginalImageUrl = "saved_locally",
            CreatedAt = DateTime.UtcNow
        };

        await clothingRepository.AddAsync(newItem, ct);
        return newItem;
    }

    private ClothingType MapToClothingType(string? label)
    {
        var cleanLabel = label?.ToLower().Replace("type_", "").Trim();
        
        return cleanLabel switch
        {
            "shirts" or "tops" or "tshirts" => ClothingType.Top,
            "pants" or "trousers" or "skirts" => ClothingType.Bottom, 
            "shoes" or "heels" or "sports shoes" or "casual shoes" => ClothingType.Shoes,
            "handbags" or "watches" or "sunglasses" => ClothingType.Accessory,
            _ => ClothingType.Outerwear
        };
    }
}
