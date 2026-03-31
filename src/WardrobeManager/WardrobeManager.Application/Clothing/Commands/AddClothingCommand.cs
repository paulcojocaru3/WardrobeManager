using MediatR;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Clothing.Commands;

public record AddClothingCommand(
    Guid UserId,
    string Name,
    ClothingType Type,
    string? Color,
    string? Gender,
    string? Season,
    string? Usage,
    string ProcessedImageB64,
    float[]? Embedding
) : IRequest<ClothingItem>;
