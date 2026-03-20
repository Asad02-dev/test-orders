using Microsoft.Extensions.Logging;

namespace Notifications.Application.Services;

public class NotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public async Task SendOrderConfirmationAsync(Guid orderId, string email, string name, decimal amount, CancellationToken ct)
    {
        _logger.LogInformation(
            "Sending order confirmation to {Email} for order {OrderId} (amount: {Amount})",
            email, orderId, amount);

        // TODO: Integrate with real email provider (SendGrid, SMTP, etc.)
        await Task.Delay(10, ct);
        _logger.LogInformation("Order confirmation sent to {Email}", email);
    }

    public async Task SendOrderCancelledAsync(Guid orderId, string email, string reason, CancellationToken ct)
    {
        _logger.LogInformation(
            "Sending order cancellation notification to {Email} for order {OrderId}",
            email, orderId);

        await Task.Delay(10, ct);
        _logger.LogInformation("Order cancellation notification sent to {Email}", email);
    }
}
