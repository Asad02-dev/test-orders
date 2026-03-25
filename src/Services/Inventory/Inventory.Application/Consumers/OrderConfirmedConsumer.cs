using Contracts.Events.Order;
using Inventory.Application.DTOs;
using Inventory.Application.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Consumers;

public class OrderConfirmedConsumer : IConsumer<OrderConfirmedEvent>
{
    private readonly InventoryService _inventoryService;
    private readonly ILogger<OrderConfirmedConsumer> _logger;

    public OrderConfirmedConsumer(InventoryService inventoryService, ILogger<OrderConfirmedConsumer> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderConfirmedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation(
            "Order {OrderId} confirmed. Committing inventory reservations for {ItemCount} item(s).",
            evt.OrderId, evt.Items.Count);

        var commitItems = evt.Items
            .Select(i => new CommitReservationItemRequest(i.ProductId, i.Quantity))
            .ToList();

        await _inventoryService.CommitReservationAsync(evt.OrderId, commitItems, context.CancellationToken);
    }
}
