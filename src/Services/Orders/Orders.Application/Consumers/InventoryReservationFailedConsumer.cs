using Contracts.Events.Inventory;
using MassTransit;
using Microsoft.Extensions.Logging;
using Orders.Application.Services;

namespace Orders.Application.Consumers;

public class InventoryReservationFailedConsumer : IConsumer<InventoryReservationFailedEvent>
{
    private readonly OrderService _orderService;
    private readonly ILogger<InventoryReservationFailedConsumer> _logger;

    public InventoryReservationFailedConsumer(OrderService orderService, ILogger<InventoryReservationFailedConsumer> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InventoryReservationFailedEvent> context)
    {
        var evt = context.Message;
        _logger.LogWarning(
            "Inventory reservation failed for order {OrderId}. Reason: {Reason}",
            evt.OrderId, evt.Reason);
        await _orderService.HandleInventoryReservationFailedAsync(evt.OrderId, evt.Reason, context.CancellationToken);
    }
}
