using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Outfits.Commands;

public class DeleteOutfitCommandHandler(
    IOutfitRepository outfitRepository,
    IValidator<DeleteOutfitCommand> validator) : IRequestHandler<DeleteOutfitCommand, bool>
{
    public async Task<bool> Handle(DeleteOutfitCommand request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        
        var outfit = await outfitRepository.GetByIdAsync(request.Id, ct);
        if (outfit == null)
        {
            return false;
        }

        await outfitRepository.DeleteAsync(outfit, ct);
        return true;
    }
}
