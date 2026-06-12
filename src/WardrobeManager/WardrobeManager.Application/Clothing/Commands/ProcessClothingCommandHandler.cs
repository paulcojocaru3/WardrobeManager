using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing.Queries;

namespace WardrobeManager.Application.Clothing.Commands;

public sealed class ProcessClothingCommandHandler(
    IMlService mlService,
    IValidator<ProcessClothingCommand> validator)
    : IRequestHandler<ProcessClothingCommand, ProcessedClothingDto>
{
    public async Task<ProcessedClothingDto> Handle(ProcessClothingCommand request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        using var stream = new MemoryStream(request.FileContent);
        var ml = await mlService.ProcessClothingImageAsync(stream, request.FileName, request.ContentType, ct);

        return new ProcessedClothingDto(
            request.Name,
            ArticleTypeMap.ToClothingType(ml.Type),
            ArticleTypeMap.Normalize(ml.Type),
            ml.Color,
            ml.Gender,
            ml.Season,
            ml.Usage,
            ml.ProcessedImageB64!,
            ml.Embedding
        );
    }
}
