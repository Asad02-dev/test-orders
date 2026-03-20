using Contracts.Events.Notification;
using MassTransit;
using Microsoft.Extensions.Logging;
using Notifications.Application.Services;

namespace Notifications.Application.Consumers;

public class OrderCancelledNotificationConsumer : IConsumer<SendOrderCancelledNotificationCommand>
{
    private readonly NotificationService _notificationService;
    private readonly ILogger<OrderCancelledNotificationConsumer> _logger;

    public OrderCancelledNotificationConsumer(
        NotificationService notificationService,
        ILogger<OrderCancelledNotificationConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SendOrderCancelledNotificationCommand> context)
    {
        var cmd = context.Message;
        _logger.LogInformation("Processing order cancellation notification for order {OrderId}", cmd.OrderId);
        await _notificationService.SendOrderCancelledAsync(
            cmd.OrderId, cmd.CustomerEmail, cmd.Reason, context.CancellationToken);
    }
}
