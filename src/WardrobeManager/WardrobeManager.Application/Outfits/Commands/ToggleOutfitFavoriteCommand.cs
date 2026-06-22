using MediatR;

namespace WardrobeManager.Application.Outfits.Commands;

public record ToggleOutfitFavoriteCommand(Guid UserId, Guid Id) : IRequest<bool?>;
