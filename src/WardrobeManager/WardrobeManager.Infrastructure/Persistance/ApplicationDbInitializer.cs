using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Infrastructure.Persistance;

public sealed class ApplicationDbInitializer(
    ApplicationDbContext db,
    ILogger<ApplicationDbInitializer> logger) : IApplicationDbInitializer
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync(ct);
                break;
            }
            catch (Exception ex) when (attempt < 5 && !ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Database not ready (attempt {Attempt}/5); retrying in 5s.", attempt);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }

        await SchemaPatcher.ApplyAdditiveColumnsAsync(db, ct);
        await SchemaPatcher.RemoveRetiredDataAsync(db, ct);
    }
}
