using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Clothing.Commands;

public record UpdateClothingCommand(
    Guid Id,
    Guid UserId,
    string Name,
    ClothingType Type,
    string? Color,
    string? Gender,
    string? Season,
    string? Usage,
    string? SubType = null
) : IRequest<bool>;

public sealed class UpdateClothingCommandHandler : IRequestHandler<UpdateClothingCommand, bool>
{
    private readonly IClothingRepository _repository;

    public UpdateClothingCommandHandler(IClothingRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(UpdateClothingCommand request, CancellationToken ct)
    {
        var item = await _repository.GetByIdForUserAsync(request.Id, request.UserId, ct);
        if (item == null) return false;

        item.UpdateDetails(
            request.Name,
            request.Type,
            request.SubType,
            request.Color,
            request.Gender,
            request.Season,
            request.Usage);

        await _repository.UpdateAsync(item);
        return true;
    }
}
