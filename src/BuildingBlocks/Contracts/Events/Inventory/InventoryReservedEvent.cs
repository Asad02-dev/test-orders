namespace Contracts.Events.Inventory;

public record InventoryReservedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public List<ReservedItemDto> Items { get; init; } = new();
}

public record ReservedItemDto
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}
