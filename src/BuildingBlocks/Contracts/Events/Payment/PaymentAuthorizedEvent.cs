namespace Contracts.Events.Payment;

public record PaymentAuthorizedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
}
