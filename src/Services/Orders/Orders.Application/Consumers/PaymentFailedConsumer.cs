using Contracts.Events.Payment;
using MassTransit;
using Microsoft.Extensions.Logging;
using Orders.Application.Services;

namespace Orders.Application.Consumers;

public class PaymentFailedConsumer : IConsumer<PaymentFailedEvent>
{
    private readonly OrderService _orderService;
    private readonly ILogger<PaymentFailedConsumer> _logger;

    public PaymentFailedConsumer(OrderService orderService, ILogger<PaymentFailedConsumer> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var evt = context.Message;
        _logger.LogWarning(
            "Payment failed for order {OrderId}. Reason: {Reason}",
            evt.OrderId, evt.Reason);
        await _orderService.HandlePaymentFailedAsync(evt.OrderId, evt.Reason, context.CancellationToken);
    }
}
