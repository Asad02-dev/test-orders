using Contracts.Events.Inventory;
using Inventory.Application.DTOs;
using Inventory.Domain.Repositories;
using MassTransit;
using SharedKernel.Common;
using SharedKernel.Interfaces;

namespace Inventory.Application.Services;

public class InventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public InventoryService(
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<InventoryItemDto?> GetByProductIdAsync(Guid productId, CancellationToken ct)
    {
        var item = await _inventoryRepository.GetByProductIdAsync(productId, ct);
        return item is null ? null : MapToDto(item);
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetLowStockAsync(CancellationToken ct)
    {
        var items = await _inventoryRepository.GetLowStockItemsAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    public async Task<Result<InventoryItemDto>> CreateAsync(CreateInventoryItemRequest request, CancellationToken ct)
    {
        var existing = await _inventoryRepository.GetByProductIdAsync(request.ProductId, ct);
        if (existing is not null)
            return Result.Failure<InventoryItemDto>($"Inventory item for product {request.ProductId} already exists.");

        var item = Inventory.Domain.Entities.InventoryItem.Create(
            request.ProductId, request.ProductName, request.Quantity, request.ReorderThreshold);
        await _inventoryRepository.AddAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(MapToDto(item));
    }

    public async Task<Result<InventoryItemDto>> RestockAsync(Guid productId, RestockRequest request, CancellationToken ct)
    {
        var item = await _inventoryRepository.GetByProductIdAsync(productId, ct);
        if (item is null)
            return Result.Failure<InventoryItemDto>($"Inventory item for product {productId} not found.");

        item.Restock(request.Quantity);
        _inventoryRepository.Update(item);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(MapToDto(item));
    }

    public async Task ProcessReservationAsync(ReservationRequest request, CancellationToken ct)
    {
        var productIds = request.Items.Select(i => i.ProductId).ToList();
        var inventoryItems = await _inventoryRepository.GetByProductIdsAsync(productIds, ct);

        var failedItems = new List<string>();
        foreach (var reqItem in request.Items)
        {
            var invItem = inventoryItems.FirstOrDefault(i => i.ProductId == reqItem.ProductId);
            if (invItem is null || invItem.AvailableQuantity < reqItem.Quantity)
                failedItems.Add(reqItem.ProductId.ToString());
        }

        if (failedItems.Count > 0)
        {
            await _publishEndpoint.Publish(new InventoryReservationFailedEvent
            {
                OrderId = request.OrderId,
                Reason = $"Insufficient stock for products: {string.Join(", ", failedItems)}"
            });
            return;
        }

        foreach (var reqItem in request.Items)
        {
            var invItem = inventoryItems.First(i => i.ProductId == reqItem.ProductId);
            invItem.TryReserve(reqItem.Quantity);
            _inventoryRepository.Update(invItem);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        await _publishEndpoint.Publish(new InventoryReservedEvent
        {
            OrderId = request.OrderId,
            CustomerId = request.CustomerId,
            TotalAmount = request.TotalAmount,
            Items = request.Items.Select(i => new ReservedItemDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        });
    }

    private static InventoryItemDto MapToDto(Inventory.Domain.Entities.InventoryItem i) => new(
        i.Id, i.ProductId, i.ProductName,
        i.QuantityOnHand, i.QuantityReserved, i.AvailableQuantity,
        i.ReorderThreshold, i.UpdatedAt);
}
