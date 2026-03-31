using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Queries;

namespace WardrobeManager.Application.Outfits.Commands;

public class GenerateAiOutfitCommandHandler(
    IOutfitGenerator outfitGenerator,
    IValidator<GenerateAiOutfitCommand> validator) : IRequestHandler<GenerateAiOutfitCommand, AiGeneratedOutfitDto>
{
    public async Task<AiGeneratedOutfitDto> Handle(GenerateAiOutfitCommand request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return await outfitGenerator.GenerateAiOutfitAsync(request.UserId, request.StartItemId, request.Threshold, ct);
    }
}
