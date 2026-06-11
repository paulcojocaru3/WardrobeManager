using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Clothing.Commands;

public sealed class DeleteClothingCommandHandler(IClothingRepository clothingRepository, IValidator<DeleteClothingCommand> validator) : IRequestHandler<DeleteClothingCommand, bool>
{
    public async Task<bool> Handle(DeleteClothingCommand request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var item = await clothingRepository.GetByIdAsync(request.Id, ct);
        if (item == null)
        {
            throw new InvalidOperationException($"Clothing item with ID {request.Id} was not found.");
        }

        await clothingRepository.DeleteAsync(item, ct);
        return true;
    }
}
