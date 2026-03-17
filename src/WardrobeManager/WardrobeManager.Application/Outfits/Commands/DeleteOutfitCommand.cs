using MediatR;

namespace WardrobeManager.Application.Outfits.Commands;

public record DeleteOutfitCommand(Guid Id) : IRequest<bool>;
