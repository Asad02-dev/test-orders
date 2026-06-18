using Contracts.Events.Notification;
using MassTransit;
using Microsoft.Extensions.Logging;
using Notifications.Application.Services;

namespace Notifications.Application.Consumers;

public class OrderConfirmationNotificationConsumer : IConsumer<SendOrderConfirmationNotificationCommand>
{
    private readonly NotificationService _notificationService;
    private readonly ILogger<OrderConfirmationNotificationConsumer> _logger;

    public OrderConfirmationNotificationConsumer(
        NotificationService notificationService,
        ILogger<OrderConfirmationNotificationConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SendOrderConfirmationNotificationCommand> context)
    {
        var cmd = context.Message;
        _logger.LogInformation("Processing order confirmation notification for order {OrderId}", cmd.OrderId);
        await _notificationService.SendOrderConfirmationAsync(
            cmd.OrderId, cmd.CustomerEmail, cmd.CustomerName, cmd.TotalAmount, context.CancellationToken);
    }
}
