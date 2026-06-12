using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Commands;

public sealed class CreateOutfitCommandHandler(
    IOutfitRepository outfitRepository, 
    IClothingRepository clothingRepository,
    IValidator<CreateOutfitCommand> validator) : IRequestHandler<CreateOutfitCommand, Guid>
{
    public async Task<Guid> Handle(CreateOutfitCommand request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var items = await clothingRepository.GetByIdsAsync(request.ItemIds, ct);

        var outfit = new Outfit
        {
            UserId = request.UserId,
            Name = request.Name,
            IsAiGenerated = request.IsAiGenerated,
            IsEventExclusive = request.IsEventExclusive,
            Tags = request.Tags ?? new List<string>(),
            Items = items,
            CreatedAt = DateTime.UtcNow
        };

        await outfitRepository.AddAsync(outfit, ct);
        return outfit.Id;
    }
}
