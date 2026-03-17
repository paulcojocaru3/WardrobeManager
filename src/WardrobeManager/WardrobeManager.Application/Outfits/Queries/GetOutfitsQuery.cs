using MediatR;

namespace WardrobeManager.Application.Outfits.Queries;

public record GetOutfitsQuery(Guid UserId) : IRequest<List<OutfitDto>>;
