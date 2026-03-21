namespace Contracts.Events.Inventory;

public record InventoryReservationFailedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
