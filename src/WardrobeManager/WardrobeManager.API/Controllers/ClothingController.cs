using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WardrobeManager.API.Extensions;
using WardrobeManager.Application.Clothing.Commands;
using WardrobeManager.Application.Clothing.Queries;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ClothingController(IMediator mediator) : ControllerBase
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetByUser(Guid userId, CancellationToken ct)
    {
        if (userId != User.GetUserId()) return Forbid();
        var items = await mediator.Send(new GetClothingItemsQuery(User.GetUserId()), ct);
        return Ok(items);
    }

    [HttpGet("{id}/similar")]
    public async Task<IActionResult> GetSimilar(Guid id, [FromQuery] int limit = 8, [FromQuery] bool sameTypeOnly = false, CancellationToken ct = default)
    {
        var results = await mediator.Send(new FindSimilarItemsQuery(User.GetUserId(), id, limit, sameTypeOnly), ct);
        return Ok(results);
    }

    [HttpGet("subtypes")]
    public async Task<IActionResult> GetSubtypes(CancellationToken ct)
    {
        var subtypes = await mediator.Send(new GetArticleSubtypesQuery(), ct);
        return Ok(subtypes);
    }

    [HttpPost("process")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Process([FromForm] IFormFile File, [FromForm] Guid UserId, [FromForm] string Name, CancellationToken ct)
    {
        var result = await mediator.Send(await ToProcessCommandAsync(File, Name, ct), ct);
        return Ok(result);
    }

    // adapts the web-layer IFormFile into a framework-neutral command (keeps IFormFile out of Application).
    private async Task<ProcessClothingCommand> ToProcessCommandAsync(IFormFile file, string name, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            throw new BadHttpRequestException("A non-empty image file is required.");
        }
        if (file.Length > MaxUploadBytes)
        {
            throw new BadHttpRequestException("Image file is too large.", StatusCodes.Status413PayloadTooLarge);
        }
        if (!AllowedImageContentTypes.Contains(file.ContentType))
        {
            throw new BadHttpRequestException("Only JPEG, PNG, and WebP images are supported.", StatusCodes.Status415UnsupportedMediaType);
        }

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return new ProcessClothingCommand(ms.ToArray(), file.FileName, file.ContentType, User.GetUserId(), name);
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] AddClothingCommand command, CancellationToken ct)
    {
        var item = await mediator.Send(command with { UserId = User.GetUserId() }, ct);
        return Ok(item);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Upload([FromForm] IFormFile File, [FromForm] Guid UserId, [FromForm] string Name, CancellationToken ct)
    {
        // keep for backward compatibility or direct upload
        var processed = await mediator.Send(await ToProcessCommandAsync(File, Name, ct), ct);
        var item = await mediator.Send(new AddClothingCommand(
            User.GetUserId(),
            Name,
            processed.Type,
            processed.SubType,
            processed.Color,
            processed.Gender,
            processed.Season,
            processed.Usage,
            processed.ProcessedImageB64,
            processed.Embedding), ct);

        return Ok(item);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteClothingCommand(User.GetUserId(), id), ct);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClothingCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        var result = await mediator.Send(command with { UserId = User.GetUserId() }, ct);
        if (!result) return NotFound();
        return Ok();
    }
}
