using Payments.Domain.Enums;
using SharedKernel.Domain;

namespace Payments.Domain.Entities;

public class Payment : AggregateRoot<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Payment() { } // EF Core

    public static Payment Create(Guid orderId, Guid customerId, decimal amount, string idempotencyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            CustomerId = customerId,
            Amount = amount,
            Status = PaymentStatus.Pending,
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Authorize()
    {
        Status = PaymentStatus.Authorized;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void Fail(string reason)
    {
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void Capture()
    {
        if (Status != PaymentStatus.Authorized)
            throw new InvalidOperationException("Can only capture authorized payments.");
        Status = PaymentStatus.Captured;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void Refund()
    {
        if (Status is not (PaymentStatus.Authorized or PaymentStatus.Captured))
            throw new InvalidOperationException("Can only refund authorized or captured payments.");
        Status = PaymentStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }
}
