namespace Orders.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    ReservationConfirmed = 1,
    PaymentAuthorized = 2,
    Confirmed = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
    Failed = 7
}
