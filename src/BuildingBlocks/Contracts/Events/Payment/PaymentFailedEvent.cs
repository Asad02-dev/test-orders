namespace Contracts.Events.Payment;

public record PaymentFailedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
