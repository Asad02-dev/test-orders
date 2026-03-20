namespace Contracts.Events.Inventory;

public record InventoryReleasedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
}
