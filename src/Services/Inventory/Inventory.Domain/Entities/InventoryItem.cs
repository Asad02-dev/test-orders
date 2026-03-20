using SharedKernel.Domain;

namespace Inventory.Domain.Entities;

public class InventoryItem : AggregateRoot<Guid>
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int QuantityOnHand { get; private set; }
    public int QuantityReserved { get; private set; }
    public int ReorderThreshold { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public int AvailableQuantity => QuantityOnHand - QuantityReserved;

    private InventoryItem() { } // EF Core

    public static InventoryItem Create(Guid productId, string productName, int quantity, int reorderThreshold = 5)
    {
        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        return new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName,
            QuantityOnHand = quantity,
            ReorderThreshold = reorderThreshold,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public bool TryReserve(int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (AvailableQuantity < quantity) return false;

        QuantityReserved += quantity;
        UpdatedAt = DateTime.UtcNow;
        Version++;
        return true;
    }

    public void ReleaseReservation(int quantity)
    {
        QuantityReserved = Math.Max(0, QuantityReserved - quantity);
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void CommitReservation(int quantity)
    {
        QuantityOnHand -= quantity;
        QuantityReserved = Math.Max(0, QuantityReserved - quantity);
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void Restock(int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        QuantityOnHand += quantity;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }
}
