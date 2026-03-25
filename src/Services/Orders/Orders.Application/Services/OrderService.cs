using Contracts.Events.Notification;
using Contracts.Events.Order;
using MassTransit;
using Orders.Application.DTOs;
using Orders.Domain.Entities;
using Orders.Domain.Enums;
using Orders.Domain.Repositories;
using SharedKernel.Common;
using SharedKernel.Interfaces;
using ContractOrderItemDto = Contracts.Events.Order.OrderItemDto;
using AppOrderItemDto = Orders.Application.DTOs.OrderItemDto;

namespace Orders.Application.Services;

public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderService(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<PagedResult<OrderDto>> GetOrdersAsync(
        int page, int pageSize, Guid? customerId, CancellationToken ct)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 100) pageSize = 20;

        var (items, totalCount) = await _orderRepository.GetPagedAsync(page, pageSize, customerId, ct);
        return new PagedResult<OrderDto>(items.Select(MapToDto).ToList(), totalCount, page, pageSize);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid id, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(id, ct);
        return order is null ? null : MapToDto(order);
    }

    public async Task<Result<OrderDto>> PlaceOrderAsync(Guid customerId, PlaceOrderRequest request, CancellationToken ct)
    {
        // Idempotency check
        var existing = await _orderRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
        if (existing is not null)
            return Result.Success(MapToDto(existing));

        var items = request.Items.Select(i => (i.ProductId, i.ProductName, i.UnitPrice, i.Quantity)).ToList();
        var order = Order.Create(customerId, request.CustomerEmail, request.CustomerName, items, request.IdempotencyKey);

        await _orderRepository.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _publishEndpoint.Publish(new OrderPlacedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            CustomerEmail = order.CustomerEmail,
            TotalAmount = order.TotalAmount,
            IdempotencyKey = order.IdempotencyKey,
            Items = order.Items.Select(i => new ContractOrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        });

        return Result.Success(MapToDto(order));
    }

    public async Task<Result> CancelOrderAsync(Guid orderId, string reason, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null) return Result.Failure($"Order '{orderId}' not found.");

        order.Cancel(reason);
        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(ct);

        await _publishEndpoint.Publish(new OrderCancelledEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Reason = reason,
            Items = order.Items.Select(i => new ContractOrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        });

        await _publishEndpoint.Publish(new SendOrderCancelledNotificationCommand
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            Reason = reason
        });

        return Result.Success();
    }

    public async Task HandleInventoryReservedAsync(Guid orderId, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null || order.Status != OrderStatus.Pending) return;

        order.ConfirmReservation();
        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task HandleInventoryReservationFailedAsync(Guid orderId, string reason, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null || order.Status == OrderStatus.Cancelled) return;

        order.Cancel(reason);
        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(ct);

        await _publishEndpoint.Publish(new OrderCancelledEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Reason = reason,
            Items = order.Items.Select(i => new ContractOrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        });

        await _publishEndpoint.Publish(new SendOrderCancelledNotificationCommand
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            Reason = reason
        });
    }

    public async Task HandlePaymentAuthorizedAsync(Guid orderId, Guid paymentId, decimal amount, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null) return;

        // Idempotent: already confirmed
        if (order.Status == OrderStatus.Confirmed) return;

        // Advance through state machine gracefully, handling out-of-order events
        if (order.Status == OrderStatus.Pending)
            order.ConfirmReservation();
        if (order.Status == OrderStatus.ReservationConfirmed)
            order.AuthorizePayment();
        if (order.Status == OrderStatus.PaymentAuthorized)
            order.Confirm();

        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(ct);

        await _publishEndpoint.Publish(new OrderConfirmedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Items = order.Items.Select(i => new ContractOrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        });

        await _publishEndpoint.Publish(new SendOrderConfirmationNotificationCommand
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            CustomerName = order.CustomerName,
            TotalAmount = order.TotalAmount
        });
    }

    public async Task HandlePaymentFailedAsync(Guid orderId, string reason, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null || order.Status == OrderStatus.Cancelled) return;

        order.Cancel(reason);
        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(ct);

        await _publishEndpoint.Publish(new OrderCancelledEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Reason = reason,
            Items = order.Items.Select(i => new ContractOrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        });

        await _publishEndpoint.Publish(new SendOrderCancelledNotificationCommand
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            Reason = reason
        });
    }

    private static OrderDto MapToDto(Order o) => new(
        o.Id, o.CustomerId, o.CustomerEmail, o.CustomerName, o.Status, o.Status.ToString(),
        o.TotalAmount,
        o.Items.Select(i => new AppOrderItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.TotalPrice)).ToList(),
        o.CreatedAt, o.UpdatedAt, o.CancellationReason
    );
}
