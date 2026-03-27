using Orders.Domain.Entities;
using Orders.Domain.Enums;

namespace Orders.Api.Tests;

public class OrderDomainTests
{
    [Fact]
    public void Order_Create_WithValidData_CreatesOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var items = new List<(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity)>
        {
            (Guid.NewGuid(), "Product 1", 50.00m, 2),
            (Guid.NewGuid(), "Product 2", 30.00m, 1)
        };

        // Act
        var order = Order.Create(
            customerId,
            "test@example.com",
            "Test Customer",
            items,
            "idempotency-key-123"
        );

        // Assert
        Assert.NotNull(order);
        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal("test@example.com", order.CustomerEmail);
        Assert.Equal("Test Customer", order.CustomerName);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(130.00m, order.TotalAmount); // (50 * 2) + (30 * 1)
        Assert.Equal("idempotency-key-123", order.IdempotencyKey);
    }

    [Fact]
    public void Order_Create_WithEmptyItems_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Order.Create(
                Guid.NewGuid(),
                "test@example.com",
                "Test Customer",
                new List<(Guid, string, decimal, int)>(),
                "key"
            )
        );
    }

    [Fact]
    public void Order_Create_WithEmptyEmail_ThrowsArgumentException()
    {
        // Arrange
        var items = new List<(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity)>
        {
            (Guid.NewGuid(), "Product", 50.00m, 1)
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Order.Create(Guid.NewGuid(), "", "Name", items, "key")
        );
    }

    [Fact]
    public void Order_ConfirmReservation_FromPending_UpdatesStatusCorrectly()
    {
        // Arrange
        var order = CreateTestOrder();
        Assert.Equal(OrderStatus.Pending, order.Status);
        var originalVersion = order.Version;

        // Act
        order.ConfirmReservation();

        // Assert
        Assert.Equal(OrderStatus.ReservationConfirmed, order.Status);
        Assert.Equal(originalVersion + 1, order.Version);
    }

    [Fact]
    public void Order_ConfirmReservation_FromNonPendingStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = CreateTestOrder();
        order.ConfirmReservation();
        Assert.Equal(OrderStatus.ReservationConfirmed, order.Status);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.ConfirmReservation());
    }

    [Fact]
    public void Order_AuthorizePayment_FromReservationConfirmed_UpdatesStatusCorrectly()
    {
        // Arrange
        var order = CreateTestOrder();
        order.ConfirmReservation();
        Assert.Equal(OrderStatus.ReservationConfirmed, order.Status);
        var originalVersion = order.Version;

        // Act
        order.AuthorizePayment();

        // Assert
        Assert.Equal(OrderStatus.PaymentAuthorized, order.Status);
        Assert.Equal(originalVersion + 1, order.Version);
    }

    [Fact]
    public void Order_AuthorizePayment_FromPendingStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = CreateTestOrder();
        Assert.Equal(OrderStatus.Pending, order.Status);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.AuthorizePayment());
    }

    [Fact]
    public void Order_Confirm_FromPaymentAuthorized_UpdatesStatusCorrectly()
    {
        // Arrange
        var order = CreateTestOrder();
        order.ConfirmReservation();
        order.AuthorizePayment();
        Assert.Equal(OrderStatus.PaymentAuthorized, order.Status);
        var originalVersion = order.Version;

        // Act
        order.Confirm();

        // Assert
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(originalVersion + 1, order.Version);
    }

    [Fact]
    public void Order_Confirm_FromPendingStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = CreateTestOrder();
        Assert.Equal(OrderStatus.Pending, order.Status);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.Confirm());
    }

    [Fact]
    public void Order_Cancel_FromPending_UpdatesStatusAndReason()
    {
        // Arrange
        var order = CreateTestOrder();
        Assert.Equal(OrderStatus.Pending, order.Status);
        var originalVersion = order.Version;

        // Act
        order.Cancel("Customer changed mind");

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Customer changed mind", order.CancellationReason);
        Assert.Equal(originalVersion + 1, order.Version);
    }

    [Fact]
    public void Order_Cancel_FromReservationConfirmed_UpdatesStatusAndReason()
    {
        // Arrange
        var order = CreateTestOrder();
        order.ConfirmReservation();
        Assert.Equal(OrderStatus.ReservationConfirmed, order.Status);

        // Act
        order.Cancel("Out of stock");

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Out of stock", order.CancellationReason);
    }

    [Fact]
    public void Order_Cancel_FromConfirmedStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = CreateTestOrder();
        order.ConfirmReservation();
        order.AuthorizePayment();
        order.Confirm();
        Assert.Equal(OrderStatus.Confirmed, order.Status);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.Cancel("Too late"));
    }

    [Fact]
    public void Order_StateTransitions_FullHappyPath_Succeeds()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act & Assert - Full state machine flow
        Assert.Equal(OrderStatus.Pending, order.Status);

        order.ConfirmReservation();
        Assert.Equal(OrderStatus.ReservationConfirmed, order.Status);

        order.AuthorizePayment();
        Assert.Equal(OrderStatus.PaymentAuthorized, order.Status);

        order.Confirm();
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    private static Order CreateTestOrder()
    {
        var items = new List<(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity)>
        {
            (Guid.NewGuid(), "Test Product", 100.00m, 1)
        };

        return Order.Create(
            Guid.NewGuid(),
            "test@example.com",
            "Test Customer",
            items,
            Guid.NewGuid().ToString()
        );
    }
}
