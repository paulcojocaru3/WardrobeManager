using MediatR;

namespace WardrobeManager.Application.Outfits.Commands;

public record GenerateOutfitCommand(Guid UserId, Guid StartItemId) : IRequest<OutfitDto>;
