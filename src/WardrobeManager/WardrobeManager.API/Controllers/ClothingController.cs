using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WardrobeManager.API.Extensions;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing;
using WardrobeManager.Application.Clothing.Commands;
using WardrobeManager.Application.Clothing.Queries;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ClothingController(IMediator mediator, IMlService mlService) : ControllerBase
{
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetByUser(Guid userId, CancellationToken ct)
    {
        if (userId != User.GetUserId()) return Forbid();
        var items = await mediator.Send(new GetClothingItemsQuery(User.GetUserId()), ct);
        return Ok(items);
    }

    [HttpGet("subtypes")]
    public async Task<IActionResult> GetSubtypes(CancellationToken ct)
    {
        var labels = await mlService.GetArticleTypesAsync(ct);
        var grouped = labels
            .Select(l => (Norm: ArticleTypeMap.Normalize(l), Type: ArticleTypeMap.TryGetClothingType(l)))
            .Where(x => x.Norm != null && x.Type != null)
            .GroupBy(x => x.Type!.Value.ToString().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.Select(x => x.Norm!).Distinct().OrderBy(s => s).ToList());
        return Ok(grouped);
    }

    [HttpPost("process")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Process([FromForm] IFormFile File, [FromForm] Guid UserId, [FromForm] string Name, CancellationToken ct)
    {
        var result = await mediator.Send(await ToProcessCommandAsync(File, Name, ct), ct);
        return Ok(result);
    }

    // Adapts the web-layer IFormFile into a framework-neutral command (keeps IFormFile out of Application).
    private async Task<ProcessClothingCommand> ToProcessCommandAsync(IFormFile file, string name, CancellationToken ct)
    {
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
    public async Task<IActionResult> Upload([FromForm] IFormFile File, [FromForm] Guid UserId, [FromForm] string Name, CancellationToken ct)
    {
        // Keep for backward compatibility or direct upload
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
        await mediator.Send(new DeleteClothingCommand(id), ct);
        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClothingCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        var result = await mediator.Send(command, ct);
        if (!result) return NotFound();
        return Ok();
    }
}
