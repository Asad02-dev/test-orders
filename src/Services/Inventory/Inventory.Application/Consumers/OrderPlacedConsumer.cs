using Contracts.Events.Order;
using Inventory.Application.DTOs;
using Inventory.Application.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Consumers;

public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly InventoryService _inventoryService;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(InventoryService inventoryService, ILogger<OrderPlacedConsumer> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Processing inventory reservation for order {OrderId}", evt.OrderId);

        var request = new ReservationRequest(
            evt.OrderId,
            evt.CustomerId,
            evt.TotalAmount,
            evt.Items.Select(i => new ReservationItemRequest(i.ProductId, i.Quantity)).ToList()
        );

        await _inventoryService.ProcessReservationAsync(request, context.CancellationToken);
    }
}
