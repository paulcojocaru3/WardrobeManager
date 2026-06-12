using MediatR;
using WardrobeManager.Application.Outfits.Generation;

namespace WardrobeManager.Application.Outfits.Commands;

public record GenerateAiOutfitCommand(Guid UserId, Guid StartItemId, double Threshold = 0.5, string? City = null, string? Style = null) : IRequest<AiGeneratedOutfitDto>;
