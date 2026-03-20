namespace Cart.Application.DTOs;

public record CartDto(
    Guid Id,
    Guid CustomerId,
    IReadOnlyList<CartItemDto> Items,
    decimal TotalAmount,
    DateTime UpdatedAt
);

public record CartItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice
);

public record AddToCartRequest(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity
);

public record UpdateCartItemRequest(
    Guid ProductId,
    int Quantity
);
