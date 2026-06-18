using Payments.Domain.Enums;

namespace Payments.Application.DTOs;

public record PaymentDto(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    PaymentStatus Status,
    string StatusName,
    string? FailureReason,
    DateTime CreatedAt
);
