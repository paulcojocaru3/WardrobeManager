using MediatR;
using WardrobeManager.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace WardrobeManager.Application.Clothing.Commands;

public record UploadClothingCommand(IFormFile File, Guid UserId, string Name) : IRequest<ClothingItem>;
