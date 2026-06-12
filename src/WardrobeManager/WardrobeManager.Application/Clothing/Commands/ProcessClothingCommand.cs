using MediatR;
using WardrobeManager.Application.Clothing.Queries;

namespace WardrobeManager.Application.Clothing.Commands;

public record ProcessClothingCommand(byte[] FileContent, string FileName, string ContentType, Guid UserId, string Name)
    : IRequest<ProcessedClothingDto>;
