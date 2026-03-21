namespace Contracts.Events.Notification;

public record SendOrderCancelledNotificationCommand : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
