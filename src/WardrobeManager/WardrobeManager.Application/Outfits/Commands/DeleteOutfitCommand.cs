using MediatR;

namespace WardrobeManager.Application.Outfits.Commands;

public record DeleteOutfitCommand(Guid UserId, Guid Id) : IRequest<bool>;
