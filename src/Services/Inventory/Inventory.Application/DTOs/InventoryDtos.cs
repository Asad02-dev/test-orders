namespace Inventory.Application.DTOs;

public record InventoryItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int QuantityOnHand,
    int QuantityReserved,
    int AvailableQuantity,
    int ReorderThreshold,
    DateTime UpdatedAt
);

public record CreateInventoryItemRequest(
    Guid ProductId,
    string ProductName,
    int Quantity,
    int ReorderThreshold = 5
);

public record RestockRequest(int Quantity);

public record ReservationRequest(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    List<ReservationItemRequest> Items
);

public record ReservationItemRequest(Guid ProductId, int Quantity);

public record CommitReservationItemRequest(Guid ProductId, int Quantity);

public record ReleaseReservationItemRequest(Guid ProductId, int Quantity);

