using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;
using WardrobeManager.Infrastructure.Repositories;

namespace WardrobeManager.Tests.Integration;

[Trait("Category", "Integration")]
[Collection(PostgresCollection.Name)]
public sealed class RepositoryTests
{
    private readonly PostgresContainerFixture _fixture;

    public RepositoryTests(PostgresContainerFixture fixture) => _fixture = fixture;

    private async Task<User> SeedUserAsync()
    {
        var user = new User { Username = $"u_{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@t.local", PasswordHash = "x" };
        await using var ctx = _fixture.CreateContext();
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task UserRepository_RoundTrips()
    {
        var user = new User { Username = $"u_{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@t.local", PasswordHash = "h" };

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new UserRepository(ctx);
            await repo.AddAsync(user);
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new UserRepository(ctx);
            Assert.NotNull(await repo.GetByIdAsync(user.Id));
            Assert.NotNull(await repo.GetByEmailAsync(user.Email));
            Assert.NotNull(await repo.GetByUsernameAsync(user.Username));

            var loaded = await repo.GetByIdAsync(user.Id);
            loaded!.PreferredCity = "Paris";
            await repo.UpdateAsync(loaded);
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new UserRepository(ctx);
            var loaded = await repo.GetByIdAsync(user.Id);
            Assert.Equal("Paris", loaded!.PreferredCity);
            await repo.DeleteAsync(loaded);
            Assert.Null(await repo.GetByIdAsync(user.Id));
        }
    }

    [Fact]
    public async Task OutfitRepository_PersistsWithItems_AndListsByUser()
    {
        var user = await SeedUserAsync();
        var top = new ClothingItem { UserId = user.Id, Name = "Top", Type = ClothingType.Top };
        var bottom = new ClothingItem { UserId = user.Id, Name = "Bottom", Type = ClothingType.Bottom };

        await using (var ctx = _fixture.CreateContext())
        {
            ctx.ClothingItems.AddRange(top, bottom);
            await ctx.SaveChangesAsync();
        }

        var outfit = new Outfit { UserId = user.Id, Name = "Look", Items = new() { top, bottom } };
        await using (var ctx = _fixture.CreateContext())
        {
            await new OutfitRepository(ctx).AddAsync(outfit, CancellationToken.None);
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new OutfitRepository(ctx);
            var loaded = await repo.GetByIdAsync(outfit.Id, CancellationToken.None);
            Assert.NotNull(loaded);
            Assert.Equal(2, loaded!.Items.Count);

            var list = await repo.GetByUserIdAsync(user.Id, CancellationToken.None);
            Assert.Single(list);

            loaded.Name = "Renamed";
            await repo.UpdateAsync(loaded, CancellationToken.None);
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new OutfitRepository(ctx);
            Assert.Equal("Renamed", (await repo.GetByIdAsync(outfit.Id, CancellationToken.None))!.Name);
            await repo.DeleteAsync(outfit, CancellationToken.None);
            Assert.Null(await repo.GetByIdAsync(outfit.Id, CancellationToken.None));
        }
    }

    [Fact]
    public async Task WearEventRepository_FiltersByDateWindow()
    {
        var user = await SeedUserAsync();
        var item = new ClothingItem { UserId = user.Id, Name = "Top", Type = ClothingType.Top };
        await using (var ctx = _fixture.CreateContext())
        {
            ctx.ClothingItems.Add(item);
            await ctx.SaveChangesAsync();
        }

        var now = DateTime.UtcNow;
        var recent = new WearEvent { UserId = user.Id, ClothingItemId = item.Id, WearDate = now.AddDays(-1) };
        var old = new WearEvent { UserId = user.Id, ClothingItemId = item.Id, WearDate = now.AddDays(-100) };

        await using (var ctx = _fixture.CreateContext())
        {
            await new WearEventRepository(ctx).AddRangeAsync(new[] { recent, old });
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new WearEventRepository(ctx);
            var windowed = await repo.GetByUserIdAsync(user.Id, now.AddDays(-7), now);
            Assert.Single(windowed);
            Assert.Equal(item.Id, windowed.First().ClothingItem!.Id); // Include populated

            var all = await repo.GetAllByUserIdAsync(user.Id);
            Assert.Equal(2, all.Count());
        }
    }

    [Fact]
    public async Task PlannerEventRepository_RoundTrips()
    {
        var user = await SeedUserAsync();
        var ev = new PlannerEvent
        {
            UserId = user.Id, Name = "Trip", Type = "Vacation", Location = "Rome",
            StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(3),
        };

        await using (var ctx = _fixture.CreateContext())
        {
            await new PlannerEventRepository(ctx).AddAsync(ev);
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new PlannerEventRepository(ctx);
            Assert.NotNull(await repo.GetByIdAsync(ev.Id));
            Assert.Single(await repo.GetByUserIdAsync(user.Id));

            var loaded = await repo.GetByIdAsync(ev.Id);
            loaded!.Status = "Archived";
            await repo.UpdateAsync(loaded);
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new PlannerEventRepository(ctx);
            Assert.Equal("Archived", (await repo.GetByIdAsync(ev.Id))!.Status);
            await repo.DeleteAsync(ev);
            Assert.Null(await repo.GetByIdAsync(ev.Id));
        }
    }

    [Fact]
    public async Task OutfitFeedbackRepository_RecordsActions_AndReadsByGeneration()
    {
        var userId = Guid.NewGuid();
        var generationId = Guid.NewGuid();
        var item1 = Guid.NewGuid();
        var item2 = Guid.NewGuid();
        var impressions = new[]
        {
            new OutfitFeedback { UserId = userId, GenerationId = generationId, ClothingItemId = item1, SlotType = ClothingType.Top, Rank = 0, Action = FeedbackAction.Shown },
            new OutfitFeedback { UserId = userId, GenerationId = generationId, ClothingItemId = item2, SlotType = ClothingType.Bottom, Rank = 0, Action = FeedbackAction.Shown },
        };

        await using (var ctx = _fixture.CreateContext())
        {
            await new OutfitFeedbackRepository(ctx).AddImpressionsAsync(impressions);
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new OutfitFeedbackRepository(ctx);
            await repo.RecordActionAsync(userId, generationId, item1, FeedbackAction.Accepted);
            // batch-mark both items Worn
            await repo.RecordActionsForItemsAsync(userId, generationId, new[] { item1, item2 }, FeedbackAction.Worn);
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var rows = await new OutfitFeedbackRepository(ctx).GetByGenerationAsync(userId, generationId);
            Assert.Equal(2, rows.Count);
            Assert.All(rows, r => Assert.Equal(FeedbackAction.Worn, r.Action));
        }
    }

    [Fact]
    public async Task ItemPairScoreRepository_AccruesAndExposesCompatibility()
    {
        var userId = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var (lo, hi) = ItemPair.Canonical(a, b);

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new ItemPairScoreRepository(ctx);
            // one positive observation -> below the >=2 gate, not yet exposed
            await repo.UpsertBatchAsync(userId, new[] { new ItemPairDelta(a, b, 1, 1, 0) });
            Assert.Empty(await repo.GetCompatibilityMapAsync(userId));

            // a second positive observation -> now trusted
            await repo.UpsertBatchAsync(userId, new[] { new ItemPairDelta(b, a, 1, 1, 0) });
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var map = await new ItemPairScoreRepository(ctx).GetCompatibilityMapAsync(userId);
            Assert.True(map.TryGetValue((lo, hi), out var compat));
            Assert.Equal(1.0, compat, 3);
        }
    }

    [Fact]
    public async Task UserLearningProfileRepository_InsertsThenUpdates()
    {
        var userId = Guid.NewGuid();

        await using (var ctx = _fixture.CreateContext())
        {
            await new UserLearningProfileRepository(ctx).UpsertAsync(new UserLearningProfile
            {
                UserId = userId, ColorScores = new() { ["blue"] = 0.7 }, StyleScores = new() { ["casual"] = 0.6 },
            });
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new UserLearningProfileRepository(ctx);
            var loaded = await repo.GetByUserIdAsync(userId);
            Assert.NotNull(loaded);
            Assert.Equal(0.7, loaded!.ColorScores["blue"], 3);

            loaded.ColorScores["blue"] = 0.9;
            await repo.UpsertAsync(loaded);
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var loaded = await new UserLearningProfileRepository(ctx).GetByUserIdAsync(userId);
            Assert.Equal(0.9, loaded!.ColorScores["blue"], 3);
        }
    }

    [Fact]
    public async Task NotificationRepository_FullLifecycle()
    {
        var userId = (await SeedUserAsync()).Id;
        var now = DateTime.UtcNow;
        var older = Notification.Create(userId, "WeatherAlert", "Old", "m", null, "dedup-1", now.AddMinutes(-10));
        var newer = Notification.Create(userId, "DuplicateDetected", "New", "m", "{}", null, now);

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new NotificationRepository(ctx);
            await repo.AddAsync(older);
            await repo.AddAsync(newer);
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new NotificationRepository(ctx);
            var all = await repo.GetByUserAsync(userId, unreadOnly: false, take: 10);
            Assert.Equal(2, all.Count);
            Assert.Equal("New", all[0].Title); // newest first

            Assert.Single(await repo.GetByUserAsync(userId, unreadOnly: false, take: 1)); // take caps page
            Assert.Equal(2, await repo.GetUnreadCountAsync(userId));
            Assert.True(await repo.ExistsByDedupKeyAsync(userId, "dedup-1"));
            Assert.False(await repo.ExistsByDedupKeyAsync(userId, "missing"));

            Assert.True(await repo.MarkReadAsync(userId, newer.Id));
            Assert.False(await repo.MarkReadAsync(userId, Guid.NewGuid())); // unknown id
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new NotificationRepository(ctx);
            Assert.Equal(1, await repo.GetUnreadCountAsync(userId));
            Assert.Single(await repo.GetByUserAsync(userId, unreadOnly: true, take: 10));

            Assert.Equal(1, await repo.MarkAllReadAsync(userId)); // only the remaining unread one
            Assert.Equal(0, await repo.MarkAllReadAsync(userId)); // nothing left to mark
            Assert.Equal(0, await repo.GetUnreadCountAsync(userId));
        }
    }

    [Fact]
    public async Task ClothingRepository_GetLeastWornCandidates_OrdersNeverWornFirst_AndFiltersByStyle()
    {
        var user = await SeedUserAsync();
        var neverWorn = new ClothingItem { UserId = user.Id, Name = "Fresh", Type = ClothingType.Top, Usage = "Casual", Embedding = new float[512] };
        var worn = new ClothingItem { UserId = user.Id, Name = "Worn", Type = ClothingType.Top, Usage = "Casual", Embedding = new float[512] };
        var noEmbedding = new ClothingItem { UserId = user.Id, Name = "NoVec", Type = ClothingType.Top, Usage = "Casual" };
        var otherStyle = new ClothingItem { UserId = user.Id, Name = "Formal", Type = ClothingType.Top, Usage = "Formal", Embedding = new float[512] };

        await using (var ctx = _fixture.CreateContext())
        {
            ctx.ClothingItems.AddRange(neverWorn, worn, noEmbedding, otherStyle);
            ctx.WearEvents.Add(new WearEvent { UserId = user.Id, ClothingItemId = worn.Id, WearDate = DateTime.UtcNow.AddDays(-1) });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new ClothingRepository(ctx);

            var casual = await repo.GetLeastWornCandidatesAsync(user.Id, "Casual", limit: 10);
            Assert.Equal(new[] { neverWorn.Id, worn.Id }, casual.Select(i => i.Id)); // never-worn first, no-embedding excluded

            var all = await repo.GetLeastWornCandidatesAsync(user.Id, style: null, limit: 10);
            Assert.Contains(all, i => i.Id == otherStyle.Id); // style filter dropped
        }
    }

    [Fact]
    public async Task OutfitFeedbackRepository_GetRecentlyShownItemIds_FiltersBySinceAndSlot()
    {
        var userId = Guid.NewGuid();
        var generationId = Guid.NewGuid();
        var top = Guid.NewGuid();
        var bottom = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using (var ctx = _fixture.CreateContext())
        {
            ctx.OutfitFeedbacks.AddRange(
                new OutfitFeedback { UserId = userId, GenerationId = generationId, ClothingItemId = top, SlotType = ClothingType.Top, CreatedAt = now.AddHours(-1) },
                new OutfitFeedback { UserId = userId, GenerationId = generationId, ClothingItemId = bottom, SlotType = ClothingType.Bottom, CreatedAt = now.AddHours(-1) },
                new OutfitFeedback { UserId = userId, GenerationId = generationId, ClothingItemId = Guid.NewGuid(), SlotType = ClothingType.Top, CreatedAt = now.AddDays(-10) });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new OutfitFeedbackRepository(ctx);

            var recent = await repo.GetRecentlyShownItemIdsAsync(userId, now.AddDays(-2));
            Assert.Equal(2, recent.Count); // the 10-day-old row is excluded
            Assert.Contains(top, recent);

            var tops = await repo.GetRecentlyShownItemIdsAsync(userId, now.AddDays(-2), ClothingType.Top);
            Assert.Equal(new[] { top }, tops);
        }
    }

    [Fact]
    public async Task PlannerEventRepository_GetActiveWithUpcomingItineraries_FiltersByStatusAndDate()
    {
        var user = await SeedUserAsync();
        var today = DateTime.UtcNow.Date;

        var o1 = new Outfit { UserId = user.Id, Name = "o1" };
        var o2 = new Outfit { UserId = user.Id, Name = "o2" };
        var o3 = new Outfit { UserId = user.Id, Name = "o3" };

        var active = new PlannerEvent
        {
            UserId = user.Id, Name = "Upcoming", Type = "Trip", Location = "Rome",
            StartDate = today, EndDate = today.AddDays(3), Status = "Active",
            Itineraries = { new EventItinerary { OutfitId = o1.Id, Date = today.AddDays(1), Moment = "Day", StoredTemperature = 20f } }
        };
        var archived = new PlannerEvent
        {
            UserId = user.Id, Name = "Archived", Type = "Trip", Location = "Rome",
            StartDate = today, EndDate = today.AddDays(3), Status = "Archived",
            Itineraries = { new EventItinerary { OutfitId = o2.Id, Date = today.AddDays(1), Moment = "Day", StoredTemperature = 20f } }
        };
        var noTemp = new PlannerEvent
        {
            UserId = user.Id, Name = "NoTemp", Type = "Trip", Location = "Rome",
            StartDate = today, EndDate = today.AddDays(3), Status = "Active",
            Itineraries = { new EventItinerary { OutfitId = o3.Id, Date = today.AddDays(1), Moment = "Day", StoredTemperature = null } }
        };

        await using (var ctx = _fixture.CreateContext())
        {
            ctx.Outfits.AddRange(o1, o2, o3);
            ctx.PlannerEvents.AddRange(active, archived, noTemp);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new PlannerEventRepository(ctx);
            var result = await repo.GetActiveWithUpcomingItinerariesAsync(today);

            Assert.Single(result);
            Assert.Equal(active.Id, result[0].Id);
        }
    }

    [Fact]
    public async Task ClothingRepository_CrudAndQueries()
    {
        var user = await SeedUserAsync();
        var item = new ClothingItem
        {
            UserId = user.Id, Name = "Shirt", Type = ClothingType.Top,
            SubType = null, Embedding = new float[512], // no sub-type + has embedding
        };

        await using (var ctx = _fixture.CreateContext())
        {
            await new ClothingRepository(ctx).AddAsync(item);
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new ClothingRepository(ctx);
            Assert.NotNull(await repo.GetByIdAsync(item.Id));
            Assert.Single(await repo.GetByIdsAsync(new[] { item.Id }));
            Assert.Single(await repo.GetByUserIdAsync(user.Id));
            Assert.Single(await repo.GetMissingSubTypeWithEmbeddingAsync(user.Id));

            var loaded = await repo.GetByIdAsync(item.Id);
            loaded!.Name = "Renamed";
            await repo.UpdateAsync(loaded);
            loaded.SubType = "tshirts";
            await repo.UpdateRangeAsync(new[] { loaded });
        }

        // wear recency: add a wear event then read it back.
        await using (var ctx = _fixture.CreateContext())
        {
            ctx.WearEvents.Add(new WearEvent { UserId = user.Id, ClothingItemId = item.Id, WearDate = DateTime.UtcNow });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var repo = new ClothingRepository(ctx);
            var recency = await repo.GetWearRecencyAsync(user.Id);
            Assert.True(recency.ContainsKey(item.Id));
            Assert.Equal("tshirts", (await repo.GetByIdAsync(item.Id))!.SubType);

            await repo.DeleteAsync(item);
            Assert.Null(await repo.GetByIdAsync(item.Id));
        }
    }
}
