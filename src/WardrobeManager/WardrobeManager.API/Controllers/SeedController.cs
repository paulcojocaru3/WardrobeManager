using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Infrastructure.Persistance;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/seed")]
public class SeedController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SeedController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("wear-events/{userId}")]
    public async Task<IActionResult> SeedWearEvents(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return BadRequest("User not found.");

        var clothes = await _context.ClothingItems.Where(c => c.UserId == userId).ToListAsync();
        if (clothes.Count == 0) return BadRequest("No clothes found for user. Please add some clothes first.");

        var random = new Random();
        var wearEvents = new List<WearEvent>();

        // Seed data for the last 180 days (6 months) to show seasonal trends
        for (int i = 0; i < 180; i++)
        {
            var date = DateTime.UtcNow.AddDays(-i);
            
            // Randomly pick 1-4 items per day
            int itemsCount = random.Next(1, 5);
            for (int j = 0; j < itemsCount; j++)
            {
                var clothing = clothes[random.Next(clothes.Count)];
                wearEvents.Add(new WearEvent
                {
                    UserId = userId,
                    ClothingItemId = clothing.Id,
                    WearDate = date
                });
            }
        }

        // Add some outfit wears specifically
        var outfits = await _context.Outfits.Where(o => o.UserId == userId).ToListAsync();
        foreach(var outfit in outfits.Take(3))
        {
            // Each of these 3 outfits was worn 5 times in the past
            for(int k = 0; k < 5; k++) {
                var date = DateTime.UtcNow.AddDays(-random.Next(1, 100));
                var items = await _context.ClothingItems.Where(c => c.Outfits.Any(o => o.Id == outfit.Id)).ToListAsync();
                foreach(var item in items) {
                    wearEvents.Add(new WearEvent { UserId = userId, ClothingItemId = item.Id, OutfitId = outfit.Id, WearDate = date });
                }
            }
        }

        await _context.WearEvents.AddRangeAsync(wearEvents);
        await _context.SaveChangesAsync();

        return Ok($"Added {wearEvents.Count} wear events across 6 months for user {user.Username}");
    }
}