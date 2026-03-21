namespace Cart.Application.DTOs;

public record CartCheckoutRequest(
    string CustomerEmail,
    string CustomerName,
    string IdempotencyKey
);

public record CartCheckoutResponse(
    Guid OrderId,
    string Status,
    decimal TotalAmount
);
