using Contracts.Events.Inventory;
using MassTransit;
using Microsoft.Extensions.Logging;
using Orders.Application.Services;

namespace Orders.Application.Consumers;

public class InventoryReservedConsumer : IConsumer<InventoryReservedEvent>
{
    private readonly OrderService _orderService;
    private readonly ILogger<InventoryReservedConsumer> _logger;

    public InventoryReservedConsumer(OrderService orderService, ILogger<InventoryReservedConsumer> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InventoryReservedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Inventory reserved for order {OrderId}. Confirming reservation.", evt.OrderId);
        await _orderService.HandleInventoryReservedAsync(evt.OrderId, context.CancellationToken);
    }
}
