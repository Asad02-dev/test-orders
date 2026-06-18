using Contracts.Events.Payment;
using MassTransit;
using Microsoft.Extensions.Logging;
using Orders.Application.Services;

namespace Orders.Application.Consumers;

public class PaymentAuthorizedConsumer : IConsumer<PaymentAuthorizedEvent>
{
    private readonly OrderService _orderService;
    private readonly ILogger<PaymentAuthorizedConsumer> _logger;

    public PaymentAuthorizedConsumer(OrderService orderService, ILogger<PaymentAuthorizedConsumer> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentAuthorizedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation(
            "Payment authorized for order {OrderId} (payment {PaymentId}, amount {Amount}). Confirming order.",
            evt.OrderId, evt.PaymentId, evt.Amount);
        await _orderService.HandlePaymentAuthorizedAsync(evt.OrderId, evt.PaymentId, evt.Amount, context.CancellationToken);
    }
}
