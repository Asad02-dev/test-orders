namespace Contracts.Events.Inventory;

public record InventoryReservedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public List<ReservedItemDto> Items { get; init; } = new();
}

public record ReservedItemDto
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}
