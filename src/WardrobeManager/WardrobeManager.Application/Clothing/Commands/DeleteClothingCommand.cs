using MediatR;

namespace WardrobeManager.Application.Clothing.Commands;

public record DeleteClothingCommand(Guid UserId, Guid Id) : IRequest<bool>;
