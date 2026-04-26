using MediatR;

namespace WardrobeManager.Application.Outfits.Commands;

public record CreateOutfitCommand(Guid UserId, string Name, List<Guid> ItemIds, bool IsAiGenerated = true, bool IsEventExclusive = false) : IRequest<Guid>;
