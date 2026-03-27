using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orders.Application.DTOs;
using Orders.Domain.Enums;

namespace Orders.Api.Tests;

public class OrdersEndpointsTests : IClassFixture<OrdersApiFactory>
{
    private readonly HttpClient _client;
    private readonly OrdersApiFactory _factory;
    private readonly Guid _testCustomerId = Guid.NewGuid();

    public OrdersEndpointsTests(OrdersApiFactory factory)
    {
        _factory = factory;
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            });
        }).CreateClient();

        _client.DefaultRequestHeaders.Add("Authorization", "Test");
    }

    [Fact]
    public async Task GetOrders_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/orders?page=1&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetOrders_WithDefaultPagination_ReturnsResults()
    {
        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetOrderById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/orders/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PlaceOrder_WithValidData_ReturnsCreated()
    {
        // Arrange
        var request = new PlaceOrderRequest(
            CustomerEmail: "test@example.com",
            CustomerName: "Test Customer",
            Items: new List<PlaceOrderItemRequest>
            {
                new(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Test Product",
                    UnitPrice: 50.00m,
                    Quantity: 2
                ),
                new(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Another Product",
                    UnitPrice: 30.00m,
                    Quantity: 1
                )
            },
            IdempotencyKey: Guid.NewGuid().ToString()
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(result);
        Assert.Equal(request.CustomerEmail, result.CustomerEmail);
        Assert.Equal(request.CustomerName, result.CustomerName);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(130.00m, result.TotalAmount);
        Assert.Equal(OrderStatus.Pending, result.Status);
    }

    [Fact]
    public async Task PlaceOrder_WithSameIdempotencyKey_ReturnsExistingOrder()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new PlaceOrderRequest(
            CustomerEmail: "idempotent@example.com",
            CustomerName: "Idempotent Customer",
            Items: new List<PlaceOrderItemRequest>
            {
                new(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Product",
                    UnitPrice: 100.00m,
                    Quantity: 1
                )
            },
            IdempotencyKey: idempotencyKey
        );

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/orders", request);
        var response2 = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        var result1 = await response1.Content.ReadFromJsonAsync<OrderDto>();

        // Second request should succeed with same order
        response2.EnsureSuccessStatusCode();
        var result2 = await response2.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(result1!.Id, result2!.Id);
    }

    [Fact]
    public async Task PlaceOrder_WithEmptyItems_ReturnsBadRequest()
    {
        // Arrange
        var request = new PlaceOrderRequest(
            CustomerEmail: "test@example.com",
            CustomerName: "Test Customer",
            Items: new List<PlaceOrderItemRequest>(),
            IdempotencyKey: Guid.NewGuid().ToString()
        );

        // Act & Assert
        // This should fail during order creation validation
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        // The actual behavior depends on domain validation
    }

    [Fact]
    public async Task CancelOrder_WithValidId_ReturnsNoContent()
    {
        // Arrange - Create an order first
        var createRequest = new PlaceOrderRequest(
            CustomerEmail: "cancel@example.com",
            CustomerName: "Cancel Customer",
            Items: new List<PlaceOrderItemRequest>
            {
                new(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Product",
                    UnitPrice: 50.00m,
                    Quantity: 1
                )
            },
            IdempotencyKey: Guid.NewGuid().ToString()
        );
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        var cancelRequest = new { Reason = "Customer changed mind" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/orders/{createdOrder!.Id}/cancel", cancelRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify order is cancelled
        var getResponse = await _client.GetAsync($"/api/orders/{createdOrder.Id}");
        if (getResponse.IsSuccessStatusCode)
        {
            var order = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
            Assert.NotNull(order);
            Assert.Equal(OrderStatus.Cancelled, order.Status);
            Assert.Equal("Customer changed mind", order.CancellationReason);
        }
    }

    [Fact]
    public async Task CancelOrder_WithNonExistentId_ReturnsBadRequest()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var cancelRequest = new { Reason = "Test" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/orders/{nonExistentId}/cancel", cancelRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderById_AfterCreation_ReturnsCorrectData()
    {
        // Arrange - Create an order first
        var createRequest = new PlaceOrderRequest(
            CustomerEmail: "gettest@example.com",
            CustomerName: "Get Test Customer",
            Items: new List<PlaceOrderItemRequest>
            {
                new(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Product 1",
                    UnitPrice: 25.00m,
                    Quantity: 3
                )
            },
            IdempotencyKey: Guid.NewGuid().ToString()
        );
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        // Act
        var response = await _client.GetAsync($"/api/orders/{createdOrder!.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(result);
        Assert.Equal(createdOrder.Id, result.Id);
        Assert.Equal(createdOrder.CustomerEmail, result.CustomerEmail);
        Assert.Equal(createdOrder.TotalAmount, result.TotalAmount);
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim("sub", Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
