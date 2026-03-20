using Microsoft.EntityFrameworkCore;
using Persistence.Outbox;
using SharedKernel.Interfaces;

namespace Cart.Infrastructure.Persistence;

public class CartDbContext : DbContext, IUnitOfWork
{
    public CartDbContext(DbContextOptions<CartDbContext> options) : base(options) { }

    public DbSet<Cart.Domain.Entities.Cart> Carts => Set<Cart.Domain.Entities.Cart>();
    public DbSet<Cart.Domain.Entities.CartItem> CartItems => Set<Cart.Domain.Entities.CartItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cart");

        modelBuilder.Entity<Cart.Domain.Entities.Cart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId).IsUnique();
            entity.Property(e => e.Version).IsConcurrencyToken();
            entity.HasMany(e => e.Items)
                  .WithOne()
                  .HasForeignKey(i => i.CartId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Cart.Domain.Entities.CartItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(500);
        });
    }
}
