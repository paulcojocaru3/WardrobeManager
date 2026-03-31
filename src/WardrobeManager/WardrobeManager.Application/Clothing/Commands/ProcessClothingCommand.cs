using MediatR;
using Microsoft.AspNetCore.Http;
using WardrobeManager.Application.Clothing.Queries;

namespace WardrobeManager.Application.Clothing.Commands;

public record ProcessClothingCommand(IFormFile File, Guid UserId, string Name) : IRequest<ProcessedClothingDto>;
