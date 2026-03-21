using Contracts.Events.Payment;
using MassTransit;
using Payments.Application.DTOs;
using Payments.Domain.Entities;
using Payments.Domain.Repositories;
using SharedKernel.Common;
using SharedKernel.Interfaces;

namespace Payments.Application.Services;

public class PaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<PaymentDto?> GetByOrderIdAsync(Guid orderId, CancellationToken ct)
    {
        var payment = await _paymentRepository.GetByOrderIdAsync(orderId, ct);
        return payment is null ? null : MapToDto(payment);
    }

    public async Task ProcessPaymentForOrderAsync(Guid orderId, Guid customerId, decimal amount, CancellationToken ct)
    {
        var idempotencyKey = $"payment-{orderId}";
        var existing = await _paymentRepository.GetByIdempotencyKeyAsync(idempotencyKey, ct);
        if (existing is not null) return; // idempotent

        var payment = Payment.Create(orderId, customerId, amount, idempotencyKey);
        await _paymentRepository.AddAsync(payment, ct);

        // Simulate payment processing (local dev fake provider)
        bool authorized = SimulatePaymentAuthorization(amount);

        if (authorized)
        {
            payment.Authorize();
            await _unitOfWork.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new PaymentAuthorizedEvent
            {
                OrderId = orderId,
                PaymentId = payment.Id,
                Amount = amount
            });
        }
        else
        {
            payment.Fail("Payment declined by payment provider.");
            await _unitOfWork.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new PaymentFailedEvent
            {
                OrderId = orderId,
                Reason = "Payment declined by payment provider."
            });
        }
    }

    public async Task CapturePaymentAsync(Guid orderId, CancellationToken ct)
    {
        var payment = await _paymentRepository.GetByOrderIdAsync(orderId, ct);
        if (payment is null || payment.Status != Payments.Domain.Enums.PaymentStatus.Authorized) return;

        payment.Capture();
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static bool SimulatePaymentAuthorization(decimal amount)
    {
        // Simple simulation: fail payments > 10000 for testing
        return amount <= 10000;
    }

    private static PaymentDto MapToDto(Payment p) => new(
        p.Id, p.OrderId, p.CustomerId, p.Amount,
        p.Status, p.Status.ToString(), p.FailureReason, p.CreatedAt);
}
