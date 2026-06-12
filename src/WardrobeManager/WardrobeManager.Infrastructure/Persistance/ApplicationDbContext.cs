using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WardrobeManager.Domain.Entities;
using System.Linq;
using System.Text.Json;

namespace WardrobeManager.Infrastructure.Persistance;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<ClothingItem> ClothingItems { get; set; } = null!;
    public DbSet<Outfit> Outfits { get; set; } = null!;
    public DbSet<WearEvent> WearEvents { get; set; } = null!;
    public DbSet<PlannerEvent> PlannerEvents { get; set; } = null!;
    public DbSet<EventItinerary> EventItineraries { get; set; } = null!;
    public DbSet<Recommendation> Recommendations { get; set; } = null!;
    public DbSet<OutfitFeedback> OutfitFeedbacks { get; set; } = null!;
    public DbSet<UserEvaluatorWeights> UserEvaluatorWeights { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
        });

        // ClothingItem Configuration
        modelBuilder.Entity<ClothingItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.ClothingItems)
                  .HasForeignKey(e => e.UserId);

            // Configure Embedding vector (CLIP is 512-dim)
            entity.Property(e => e.Embedding)
                  .HasColumnType("vector(512)")
                  .HasConversion(
                      v => v == null ? null : new Pgvector.Vector(v),
                      v => v == null ? null : v.ToArray()
                  )
                  .Metadata.SetValueComparer(new ValueComparer<float[]>(
                      (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                      c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                      c => c.ToArray()));

            // Add HNSW index for fast vector similarity search
            entity.HasIndex(e => e.Embedding)
                  .HasMethod("hnsw")
                  .HasOperators("vector_cosine_ops");
        });

        // Outfit Configuration (Many-to-Many with ClothingItem)
        modelBuilder.Entity<Outfit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Outfits)
                  .HasForeignKey(e => e.UserId);

            entity.HasMany(e => e.Items)
                  .WithMany(i => i.Outfits)
                  .UsingEntity(j => j.ToTable("OutfitItems"));
        });

        // WearEvent Configuration
        modelBuilder.Entity<WearEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.WearEvents)
                  .HasForeignKey(e => e.UserId);

            entity.HasOne(e => e.ClothingItem)
                  .WithMany(i => i.WearEvents)
                  .HasForeignKey(e => e.ClothingItemId);

            entity.HasOne(e => e.Outfit)
                  .WithMany()
                  .HasForeignKey(e => e.OutfitId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // PlannerEvent Configuration
        modelBuilder.Entity<PlannerEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.PlannerEvents)
                  .HasForeignKey(e => e.UserId);
        });

        // EventItinerary Configuration
        modelBuilder.Entity<EventItinerary>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.PlannerEvent)
                  .WithMany(p => p.Itineraries)
                  .HasForeignKey(e => e.PlannerEventId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Outfit)
                  .WithMany(o => o.EventItineraries)
                  .HasForeignKey(e => e.OutfitId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Recommendation Configuration
        modelBuilder.Entity<Recommendation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId);
        });

        // Reusable jsonb <-> Dictionary<string,double> converter/comparer.
        var dictConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<Dictionary<string, double>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, double>());

        var dictComparer = new ValueComparer<Dictionary<string, double>>(
            (a, b) => a != null && b != null && a.Count == b.Count && !a.Except(b).Any(),
            c => c.Aggregate(0, (h, kv) => HashCode.Combine(h, kv.Key.GetHashCode(), kv.Value.GetHashCode())),
            c => new Dictionary<string, double>(c));

        // OutfitFeedback Configuration
        modelBuilder.Entity<OutfitFeedback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.GenerationId });
            entity.Property(e => e.Action).HasConversion<string>();
            entity.Property(e => e.EvaluatorScores)
                  .HasColumnType("jsonb")
                  .HasConversion(dictConverter)
                  .Metadata.SetValueComparer(dictComparer);
        });

        // UserEvaluatorWeights Configuration (one row per user)
        modelBuilder.Entity<UserEvaluatorWeights>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.Weights)
                  .HasColumnType("jsonb")
                  .HasConversion(dictConverter)
                  .Metadata.SetValueComparer(dictComparer);
        });
    }
}