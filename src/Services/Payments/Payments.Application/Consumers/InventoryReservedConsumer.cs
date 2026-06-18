using Contracts.Events.Inventory;
using MassTransit;
using Microsoft.Extensions.Logging;
using Payments.Application.Services;

namespace Payments.Application.Consumers;

public class InventoryReservedConsumer : IConsumer<InventoryReservedEvent>
{
    private readonly PaymentService _paymentService;
    private readonly ILogger<InventoryReservedConsumer> _logger;

    public InventoryReservedConsumer(PaymentService paymentService, ILogger<InventoryReservedConsumer> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InventoryReservedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation(
            "Inventory reserved for order {OrderId}. Processing payment for customer {CustomerId}, amount {Amount}.",
            evt.OrderId, evt.CustomerId, evt.TotalAmount);

        await _paymentService.ProcessPaymentForOrderAsync(
            evt.OrderId, evt.CustomerId, evt.TotalAmount, context.CancellationToken);
    }
}
