using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Seeding.Commands;

// dev tool: seeds 6 months of plausible wear history for a user
public record SeedWearEventsCommand(Guid UserId) : IRequest<SeedWearEventsResult>;

public record SeedWearEventsResult(int EventsAdded, string Username);

public sealed class SeedWearEventsCommandHandler(
    IUserRepository userRepository,
    IClothingRepository clothingRepository,
    IOutfitRepository outfitRepository,
    IWearEventRepository wearEventRepository) : IRequestHandler<SeedWearEventsCommand, SeedWearEventsResult>
{
    public async Task<SeedWearEventsResult> Handle(SeedWearEventsCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        var clothes = await clothingRepository.GetByUserIdAsync(request.UserId, ct);
        if (clothes.Count == 0)
            throw new InvalidOperationException("No clothes found for user. Please add some clothes first.");

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
                    UserId = request.UserId,
                    ClothingItemId = clothing.Id,
                    WearDate = date
                });
            }
        }

        // Add some outfit wears specifically: 3 outfits worn 5 times each in the past
        var outfits = await outfitRepository.GetByUserIdAsync(request.UserId, ct);
        foreach (var outfit in outfits.Take(3))
        {
            for (int k = 0; k < 5; k++)
            {
                var date = DateTime.UtcNow.AddDays(-random.Next(1, 100));
                foreach (var item in outfit.Items)
                {
                    wearEvents.Add(new WearEvent
                    {
                        UserId = request.UserId,
                        ClothingItemId = item.Id,
                        OutfitId = outfit.Id,
                        WearDate = date
                    });
                }
            }
        }

        await wearEventRepository.AddRangeAsync(wearEvents, ct);
        return new SeedWearEventsResult(wearEvents.Count, user.Username);
    }
}
