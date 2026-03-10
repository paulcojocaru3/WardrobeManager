using MediatR;

namespace WardrobeManager.Application.Clothing.Commands;

public record DeleteClothingCommand(Guid Id) : IRequest<bool>;