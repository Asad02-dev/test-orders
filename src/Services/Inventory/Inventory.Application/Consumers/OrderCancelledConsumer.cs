using Contracts.Events.Order;
using Inventory.Application.DTOs;
using Inventory.Application.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Consumers;

public class OrderCancelledConsumer : IConsumer<OrderCancelledEvent>
{
    private readonly InventoryService _inventoryService;
    private readonly ILogger<OrderCancelledConsumer> _logger;

    public OrderCancelledConsumer(InventoryService inventoryService, ILogger<OrderCancelledConsumer> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation(
            "Order {OrderId} cancelled. Releasing inventory reservations for {ItemCount} item(s). Reason: {Reason}",
            evt.OrderId, evt.Items.Count, evt.Reason);

        var releaseItems = evt.Items
            .Select(i => new ReleaseReservationItemRequest(i.ProductId, i.Quantity))
            .ToList();

        await _inventoryService.ReleaseReservationAsync(evt.OrderId, releaseItems, context.CancellationToken);
    }
}
