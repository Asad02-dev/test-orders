using Orders.Domain.Enums;
using Orders.Domain.Events;
using SharedKernel.Domain;

namespace Orders.Domain.Entities;

public class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();

    public Guid CustomerId { get; private set; }
    public string CustomerEmail { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string? CancellationReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // EF Core

    public static Order Create(
        Guid customerId,
        string customerEmail,
        string customerName,
        List<(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity)> items,
        string idempotencyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (items is null || items.Count == 0) throw new ArgumentException("Order must have at least one item.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CustomerEmail = customerEmail,
            CustomerName = string.IsNullOrWhiteSpace(customerName) ? customerEmail : customerName,
            Status = OrderStatus.Pending,
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        foreach (var (productId, productName, unitPrice, quantity) in items)
        {
            order._items.Add(OrderItem.Create(order.Id, productId, productName, unitPrice, quantity));
        }

        order.TotalAmount = order._items.Sum(i => i.TotalPrice);

        order.AddDomainEvent(new OrderCreatedDomainEvent(order.Id, customerId, customerEmail, customerName, order.TotalAmount));

        return order;
    }

    public void ConfirmReservation()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm reservation for order in {Status} status.");
        Status = OrderStatus.ReservationConfirmed;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void AuthorizePayment()
    {
        if (Status != OrderStatus.ReservationConfirmed)
            throw new InvalidOperationException($"Cannot authorize payment for order in {Status} status.");
        Status = OrderStatus.PaymentAuthorized;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void Confirm()
    {
        if (Status != OrderStatus.PaymentAuthorized)
            throw new InvalidOperationException($"Cannot confirm order in {Status} status.");
        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void Cancel(string reason)
    {
        if (Status is OrderStatus.Confirmed or OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Failed)
            throw new InvalidOperationException($"Cannot cancel order in {Status} status.");
        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }
}
