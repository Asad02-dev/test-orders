using SharedKernel.Domain;

namespace Cart.Domain.Entities;

public class CartItem : Entity<Guid>
{
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal TotalPrice => UnitPrice * Quantity;

    private CartItem() { } // EF Core

    internal static CartItem Create(Guid cartId, Guid productId, string productName, decimal unitPrice, int quantity)
    {
        return new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cartId,
            ProductId = productId,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity
        };
    }

    internal void IncreaseQuantity(int amount) => Quantity += amount;
    internal void SetQuantity(int quantity) => Quantity = quantity;
}
