using Contracts.Events.Inventory;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Payments.Application.Consumers;

public class InventoryReservedConsumer : IConsumer<InventoryReservedEvent>
{
    private readonly ILogger<InventoryReservedConsumer> _logger;

    public InventoryReservedConsumer(ILogger<InventoryReservedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InventoryReservedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation(
            "Inventory reserved for order {OrderId}. Payment processing is triggered by OrdersService or saga.",
            evt.OrderId);
        await Task.CompletedTask;
    }
}
