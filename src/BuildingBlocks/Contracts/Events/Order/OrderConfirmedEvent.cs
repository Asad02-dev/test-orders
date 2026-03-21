namespace Contracts.Events.Order;

public record OrderConfirmedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
}
