using Microsoft.EntityFrameworkCore;

namespace WardrobeManager.Infrastructure.Persistance;

// add live-db columns without recreating volumes.
public static class SchemaPatcher
{
    public static async Task ApplyAdditiveColumnsAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        const string sql = """
            ALTER TABLE "ClothingItems" ADD COLUMN IF NOT EXISTS "Material" text;
            ALTER TABLE "ClothingItems" ADD COLUMN IF NOT EXISTS "SecondaryColor" text;
            ALTER TABLE "ClothingItems" ADD COLUMN IF NOT EXISTS "Pattern" text;
            ALTER TABLE "ClothingItems" ADD COLUMN IF NOT EXISTS "Formality" integer;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "UseGemmaStylistForOutfits" boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "DefaultReuseAfterDays" integer DEFAULT 3;
            ALTER TABLE "PlannerEvents" ADD COLUMN IF NOT EXISTS "ReuseAfterDays" integer;

            -- New tables aren't created by EnsureCreated on an existing DB; create them idempotently.
            CREATE TABLE IF NOT EXISTS "Notifications" (
                "Id" uuid NOT NULL CONSTRAINT "PK_Notifications" PRIMARY KEY,
                "UserId" uuid NOT NULL,
                "Type" text NOT NULL,
                "Title" text NOT NULL,
                "Message" text NOT NULL,
                "Payload" text NULL,
                "DedupKey" text NULL,
                "IsRead" boolean NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "ReadAt" timestamp with time zone NULL,
                CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId")
                    REFERENCES "Users" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId_CreatedAt"
                ON "Notifications" ("UserId", "CreatedAt");
            CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId_IsRead"
                ON "Notifications" ("UserId", "IsRead");
            """;
        await db.Database.ExecuteSqlRawAsync(sql, ct);
    }

    public static Task RemoveRetiredDataAsync(ApplicationDbContext db, CancellationToken ct = default) =>
        db.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"Notifications\" WHERE \"Type\" = 'EnrichmentComplete';",
            ct);
}
