using MediatR;
using Microsoft.AspNetCore.Mvc;
using WardrobeManager.Application.Outfits.Commands;
using WardrobeManager.Application.Outfits.Queries;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OutfitsController(IMediator mediator) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateOutfit([FromBody] GenerateOutfitCommand command)
    {
        var outfit = await mediator.Send(command);
        return Ok(outfit);
    }

    [HttpPost("generate-ai")]
    public async Task<IActionResult> GenerateAiOutfit([FromBody] GenerateAiOutfitCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOutfit([FromBody] CreateOutfitCommand command)
    {
        var id = await mediator.Send(command);
        return Ok(id);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserOutfits(Guid userId)
    {
        var outfits = await mediator.Send(new GetOutfitsQuery(userId));
        return Ok(outfits);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOutfit(Guid id, [FromBody] UpdateOutfitCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        await mediator.Send(command);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOutfit(Guid id)
    {
        await mediator.Send(new DeleteOutfitCommand(id));
        return NoContent();
    }
}
