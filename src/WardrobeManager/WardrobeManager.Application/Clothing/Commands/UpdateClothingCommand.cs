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
    string? Usage
) : IRequest<bool>;

public class UpdateClothingCommandHandler : IRequestHandler<UpdateClothingCommand, bool>
{
    private readonly IClothingRepository _repository;

    public UpdateClothingCommandHandler(IClothingRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(UpdateClothingCommand request, CancellationToken ct)
    {
        var item = await _repository.GetByIdAsync(request.Id);
        if (item == null || item.UserId != request.UserId) return false;

        item.Name = request.Name;
        item.Type = request.Type;
        item.Color = request.Color;
        item.Gender = request.Gender;
        item.Season = request.Season;
        item.Usage = request.Usage;

        await _repository.UpdateAsync(item);
        return true;
    }
}