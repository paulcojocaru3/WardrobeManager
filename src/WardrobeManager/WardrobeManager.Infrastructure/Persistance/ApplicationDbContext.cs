using Microsoft.EntityFrameworkCore;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Infrastructure.Persistance;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<ClothingItem> ClothingItems { get; set; } = null!;
    public DbSet<Outfit> Outfits { get; set; } = null!;
    public DbSet<WearEvent> WearEvents { get; set; } = null!;
    public DbSet<Recommendation> Recommendations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

        // Recommendation Configuration
        modelBuilder.Entity<Recommendation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId);
        });
    }
}