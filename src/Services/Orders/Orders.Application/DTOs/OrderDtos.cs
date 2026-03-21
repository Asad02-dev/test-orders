using Orders.Domain.Enums;

namespace Orders.Application.DTOs;

public record OrderDto(
    Guid Id,
    Guid CustomerId,
    string CustomerEmail,
    OrderStatus Status,
    string StatusName,
    decimal TotalAmount,
    IReadOnlyList<OrderItemDto> Items,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? CancellationReason
);

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice
);

public record PlaceOrderRequest(
    string CustomerEmail,
    List<PlaceOrderItemRequest> Items,
    string IdempotencyKey
);

public record PlaceOrderItemRequest(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity
);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
};
