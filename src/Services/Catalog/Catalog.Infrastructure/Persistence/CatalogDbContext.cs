using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Outbox;
using SharedKernel.Interfaces;

namespace Catalog.Infrastructure.Persistence;

public class CatalogDbContext : DbContext, IUnitOfWork
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Sku).IsUnique();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(500);
        });

        // Seed data
        modelBuilder.Entity<Product>().HasData(
            Product.Create("Laptop Pro 15", "High-performance laptop with 15-inch display", 1299.99m, "LAPTOP-PRO-15", "Electronics", "", "USD"),
            Product.Create("Wireless Headphones", "Noise-cancelling wireless headphones", 199.99m, "WH-NC-100", "Electronics", "", "USD"),
            Product.Create("Coffee Maker Deluxe", "12-cup programmable coffee maker", 79.99m, "CM-DLX-12", "Kitchen", "", "USD")
        );
    }
}
