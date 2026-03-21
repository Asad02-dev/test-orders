using Contracts.Events.Order;
using MassTransit;
using Microsoft.Extensions.Logging;
using Payments.Application.Services;

namespace Payments.Application.Consumers;

public class OrderConfirmedConsumer : IConsumer<OrderConfirmedEvent>
{
    private readonly PaymentService _paymentService;
    private readonly ILogger<OrderConfirmedConsumer> _logger;

    public OrderConfirmedConsumer(PaymentService paymentService, ILogger<OrderConfirmedConsumer> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderConfirmedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation(
            "Order {OrderId} confirmed. Capturing authorized payment.",
            evt.OrderId);

        await _paymentService.CapturePaymentAsync(evt.OrderId, context.CancellationToken);
    }
}
