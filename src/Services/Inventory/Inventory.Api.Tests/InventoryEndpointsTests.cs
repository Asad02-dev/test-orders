using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Inventory.Application.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inventory.Api.Tests;

public class InventoryEndpointsTests : IClassFixture<InventoryApiFactory>
{
    private readonly HttpClient _client;
    private readonly InventoryApiFactory _factory;

    public InventoryEndpointsTests(InventoryApiFactory factory)
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
    public async Task GetInventoryByProductId_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/inventory/products/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateInventoryItem_WithValidData_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            QuantityOnHand = 100,
            ReorderLevel = 10,
            ReorderQuantity = 50
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/inventory", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<InventoryItemDto>();
        Assert.NotNull(result);
        Assert.Equal(request.ProductId, result.ProductId);
        Assert.Equal(request.QuantityOnHand, result.QuantityOnHand);
    }

    [Fact]
    public async Task GetInventoryByProductId_AfterCreation_ReturnsCorrectData()
    {
        // Arrange - Create inventory item first
        var productId = Guid.NewGuid();
        var createRequest = new
        {
            ProductId = productId,
            ProductName = "Test Product",
            QuantityOnHand = 100,
            ReorderLevel = 10,
            ReorderQuantity = 50
        };
        await _client.PostAsJsonAsync("/api/inventory", createRequest);

        // Act
        var response = await _client.GetAsync($"/api/inventory/products/{productId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<InventoryItemDto>();
        Assert.NotNull(result);
        Assert.Equal(productId, result.ProductId);
        Assert.Equal(100, result.QuantityOnHand);
    }

    [Fact]
    public async Task RestockProduct_WithValidData_ReturnsOk()
    {
        // Arrange - Create inventory item first
        var productId = Guid.NewGuid();
        var createRequest = new
        {
            ProductId = productId,
            ProductName = "Test Product",
            QuantityOnHand = 10,
            ReorderLevel = 10,
            ReorderQuantity = 50
        };
        await _client.PostAsJsonAsync("/api/inventory", createRequest);

        var restockRequest = new
        {
            Quantity = 50
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/inventory/products/{productId}/restock", restockRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<InventoryItemDto>();
        Assert.NotNull(result);
        Assert.Equal(60, result.QuantityOnHand); // 10 + 50
    }

    [Fact]
    public async Task GetLowStockItems_ReturnsOnlyLowStockItems()
    {
        // Arrange - Create inventory items with different stock levels
        await _client.PostAsJsonAsync("/api/inventory", new
        {
            ProductId = Guid.NewGuid(),
            ProductName = "Low Stock Product",
            QuantityOnHand = 5,
            ReorderLevel = 10,
            ReorderQuantity = 50
        });
        await _client.PostAsJsonAsync("/api/inventory", new
        {
            ProductId = Guid.NewGuid(),
            ProductName = "High Stock Product",
            QuantityOnHand = 100,
            ReorderLevel = 10,
            ReorderQuantity = 50
        });

        // Act
        var response = await _client.GetAsync("/api/inventory/low-stock");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<InventoryItemDto>>();
        Assert.NotNull(result);
        Assert.All(result, item => Assert.True(item.QuantityOnHand <= item.ReorderLevel));
    }

    [Fact]
    public async Task RestockProduct_WithNonExistentProductId_ReturnsBadRequest()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var restockRequest = new
        {
            Quantity = 50
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/inventory/products/{nonExistentId}/restock", restockRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
