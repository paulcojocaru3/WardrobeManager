using MediatR;

namespace WardrobeManager.Application.Outfits.Commands;

public record ToggleOutfitFavoriteCommand(Guid Id) : IRequest<bool>;
