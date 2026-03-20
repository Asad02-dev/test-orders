namespace Notifications.Application.DTOs;

public record NotificationRecord(
    Guid Id,
    string Type,
    string Recipient,
    string Subject,
    bool Sent,
    DateTime CreatedAt,
    DateTime? SentAt
);
