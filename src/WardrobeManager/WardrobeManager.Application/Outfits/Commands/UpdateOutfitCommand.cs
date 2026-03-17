using MediatR;

namespace WardrobeManager.Application.Outfits.Commands;

public record UpdateOutfitCommand(Guid Id, string Name, List<Guid> ItemIds) : IRequest<bool>;
