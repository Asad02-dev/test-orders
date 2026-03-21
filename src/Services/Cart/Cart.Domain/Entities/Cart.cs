using SharedKernel.Domain;

namespace Cart.Domain.Entities;

public class Cart : AggregateRoot<Guid>
{
    private readonly List<CartItem> _items = new();

    public Guid CustomerId { get; private set; }
    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public decimal TotalAmount => _items.Sum(i => i.TotalPrice);

    private Cart() { } // EF Core

    public static Cart Create(Guid customerId)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId cannot be empty.", nameof(customerId));

        return new Cart
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
        {
            existing.IncreaseQuantity(quantity);
        }
        else
        {
            _items.Add(CartItem.Create(Id, productId, productName, unitPrice, quantity));
        }
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void UpdateItemQuantity(Guid productId, int quantity)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new InvalidOperationException($"Product {productId} not in cart.");

        if (quantity <= 0)
        {
            _items.Remove(item);
        }
        else
        {
            item.SetQuantity(quantity);
        }
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void RemoveItem(Guid productId)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item is not null)
        {
            _items.Remove(item);
            UpdatedAt = DateTime.UtcNow;
            Version++;
        }
    }

    public void Clear()
    {
        _items.Clear();
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }
}
