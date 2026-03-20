namespace Contracts.Events.Notification;

public record SendOrderConfirmationNotificationCommand : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
}
