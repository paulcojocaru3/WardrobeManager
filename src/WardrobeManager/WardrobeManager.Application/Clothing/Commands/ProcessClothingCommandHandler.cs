using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing.Queries;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Clothing.Commands;

public class ProcessClothingCommandHandler(
    IMlService mlService,
    IValidator<ProcessClothingCommand> validator) 
    : IRequestHandler<ProcessClothingCommand, ProcessedClothingDto>
{
    public async Task<ProcessedClothingDto> Handle(ProcessClothingCommand request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var (typeLabel, colorLabel, processedImageB64, embedding, gender, season, usage) = await mlService.ProcessClothingImageAsync(request.File, ct);

        return new ProcessedClothingDto(
            request.Name,
            MapToClothingType(typeLabel),
            colorLabel,
            gender,
            season,
            usage,
            processedImageB64,
            embedding
        );
    }

    private ClothingType MapToClothingType(string? label)
    {
        var cleanLabel = label?.ToLower().Replace("type_", "").Trim();
        
        return cleanLabel switch
        {
            // TOPS
            "shirts" or "tops" or "tshirts" or "kurta" or "kurtas" or "tunics" or "kurtis" or "dresses" or "jumpsuit" or "rompers" or "dresses" => ClothingType.Top,
            
            // BOTTOMS
            "pants" or "trousers" or "skirts" or "jeans" or "shorts" or "track pants" or "leggings" or "jeggings" or "capris" or "tights" or "churidar" or "lounge pants" or "lounge shorts" or "patiala" or "salwar" or "rain trousers" => ClothingType.Bottom, 
            
            // SHOES
            "shoes" or "heels" or "sports shoes" or "casual shoes" or "flip flops" or "sandals" or "flats" or "formal shoes" or "booties" or "sports sandals" => ClothingType.Shoes,
            
            // ACCESSORIES
            "watches" or "sunglasses" or "belts" or "wallets" or "backpacks" or "caps" or "hat" or "bangle" or "bracelet" or "earrings" or "jewellery set" or "necklace and chains" or "pendant" or "ring" or "wristbands" or "clutches" or "headband" or "scarves" or "stoles" or "ties" or "umbrellas" => ClothingType.Accessory,
            
            // OUTERWEAR
            "jackets" or "sweaters" or "sweatshirts" or "shrug" or "rain jacket" or "waistcoat" or "blazers" or "suits" or "nehru jackets" => ClothingType.Outerwear,
            
            _ => ClothingType.Outerwear
        };
    }
}
