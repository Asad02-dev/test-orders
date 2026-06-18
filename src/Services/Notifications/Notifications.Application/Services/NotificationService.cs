using Microsoft.Extensions.Logging;
using Notifications.Application.Models;
using Notifications.Application.Repositories;

namespace Notifications.Application.Services;

public class NotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(INotificationRepository notificationRepository, ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task SendOrderConfirmationAsync(Guid orderId, string email, string name, decimal amount, CancellationToken ct)
    {
        _logger.LogInformation(
            "Sending order confirmation to {Email} for order {OrderId} (amount: {Amount})",
            email, orderId, amount);

        var log = new NotificationLog
        {
            Type = "OrderConfirmation",
            OrderId = orderId,
            Recipient = email,
            Subject = $"Order #{orderId} Confirmed",
            Body = $"Dear {name}, your order #{orderId} for {amount:C} has been confirmed.",
            Sent = true,
            SentAt = DateTime.UtcNow
        };

        await _notificationRepository.AddAsync(log, ct);
        await _notificationRepository.SaveChangesAsync(ct);

        _logger.LogInformation("Order confirmation logged for {Email} (order {OrderId})", email, orderId);
    }

    public async Task SendOrderCancelledAsync(Guid orderId, string email, string reason, CancellationToken ct)
    {
        _logger.LogInformation(
            "Sending order cancellation notification to {Email} for order {OrderId}",
            email, orderId);

        var log = new NotificationLog
        {
            Type = "OrderCancelled",
            OrderId = orderId,
            Recipient = email,
            Subject = $"Order #{orderId} Cancelled",
            Body = $"Your order #{orderId} has been cancelled. Reason: {reason}",
            Sent = true,
            SentAt = DateTime.UtcNow
        };

        await _notificationRepository.AddAsync(log, ct);
        await _notificationRepository.SaveChangesAsync(ct);

        _logger.LogInformation("Order cancellation notification logged for {Email} (order {OrderId})", email, orderId);
    }

    public async Task<IReadOnlyList<NotificationLog>> GetByOrderIdAsync(Guid orderId, CancellationToken ct)
        => await _notificationRepository.GetByOrderIdAsync(orderId, ct);

    public async Task<IReadOnlyList<NotificationLog>> GetRecentAsync(int count, CancellationToken ct)
        => await _notificationRepository.GetRecentAsync(count, ct);
}
