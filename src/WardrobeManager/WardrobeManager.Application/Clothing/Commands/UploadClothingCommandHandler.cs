using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Clothing.Commands;

public class UploadClothingCommandHandler(IClothingRepository clothingRepository, IMlService mlService) 
    : IRequestHandler<UploadClothingCommand, ClothingItem>
{
    public async Task<ClothingItem> Handle(UploadClothingCommand request, CancellationToken ct)
    {
        // 1. Procesare AI (Cere atat Type cat si Color)
        var (typeLabel, colorLabel, processedImageB64) = await mlService.ProcessClothingImageAsync(request.File, ct);

        Console.WriteLine($"[DEBUG] ML Result: Type='{typeLabel}', Color='{colorLabel}'");

        // 2. Creăm entitatea
        var newItem = new ClothingItem
        {
            UserId = request.UserId,
            Name = request.File.FileName,
            Type = MapToClothingType(typeLabel),
            Color = colorLabel, // Salvam culoarea detectata
            ProcessedImageUrl = $"data:image/png;base64,{processedImageB64}",
            OriginalImageUrl = "saved_locally",
            CreatedAt = DateTime.UtcNow
        };

        // 3. Salvăm
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