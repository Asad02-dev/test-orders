using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Cart.Application.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cart.Api.Tests;

public class CartEndpointsTests : IClassFixture<CartApiFactory>
{
    private readonly HttpClient _client;
    private readonly CartApiFactory _factory;

    public CartEndpointsTests(CartApiFactory factory)
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
    public async Task GetCart_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/cart");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CartDto>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task AddItemToCart_WithValidData_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            UnitPrice = 50.00m,
            Quantity = 2
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/cart/items", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CartDto>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(request.ProductName, result.Items[0].ProductName);
        Assert.Equal(2, result.Items[0].Quantity);
    }

    [Fact]
    public async Task AddItemToCart_TwiceWithSameProduct_UpdatesQuantity()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new
        {
            ProductId = productId,
            ProductName = "Test Product",
            UnitPrice = 50.00m,
            Quantity = 1
        };

        // Act
        await _client.PostAsJsonAsync("/api/cart/items", request);
        var response2 = await _client.PostAsJsonAsync("/api/cart/items", request);

        // Assert
        response2.EnsureSuccessStatusCode();
        var result = await response2.Content.ReadFromJsonAsync<CartDto>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Quantity);
    }

    [Fact]
    public async Task UpdateCartItem_WithValidData_ReturnsOk()
    {
        // Arrange - Add item first
        var productId = Guid.NewGuid();
        var addRequest = new
        {
            ProductId = productId,
            ProductName = "Test Product",
            UnitPrice = 50.00m,
            Quantity = 1
        };
        await _client.PostAsJsonAsync("/api/cart/items", addRequest);

        var updateRequest = new
        {
            ProductId = productId,
            Quantity = 5
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/cart/items", updateRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CartDto>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(5, result.Items[0].Quantity);
    }

    [Fact]
    public async Task RemoveCartItem_WithValidProductId_ReturnsNoContent()
    {
        // Arrange - Add item first
        var productId = Guid.NewGuid();
        var addRequest = new
        {
            ProductId = productId,
            ProductName = "Test Product",
            UnitPrice = 50.00m,
            Quantity = 1
        };
        await _client.PostAsJsonAsync("/api/cart/items", addRequest);

        // Act
        var response = await _client.DeleteAsync($"/api/cart/items/{productId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify item is removed
        var getResponse = await _client.GetAsync("/api/cart");
        var cart = await getResponse.Content.ReadFromJsonAsync<CartDto>();
        Assert.NotNull(cart);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public async Task ClearCart_RemovesAllItems()
    {
        // Arrange - Add multiple items
        await _client.PostAsJsonAsync("/api/cart/items", new
        {
            ProductId = Guid.NewGuid(),
            ProductName = "Product 1",
            UnitPrice = 50.00m,
            Quantity = 1
        });
        await _client.PostAsJsonAsync("/api/cart/items", new
        {
            ProductId = Guid.NewGuid(),
            ProductName = "Product 2",
            UnitPrice = 30.00m,
            Quantity = 2
        });

        // Act
        var response = await _client.DeleteAsync("/api/cart");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify cart is empty
        var getResponse = await _client.GetAsync("/api/cart");
        var cart = await getResponse.Content.ReadFromJsonAsync<CartDto>();
        Assert.NotNull(cart);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public async Task CheckoutCart_WithValidCart_ReturnsSuccess()
    {
        // Arrange - Add items to cart
        await _client.PostAsJsonAsync("/api/cart/items", new
        {
            ProductId = Guid.NewGuid(),
            ProductName = "Product 1",
            UnitPrice = 50.00m,
            Quantity = 1
        });

        var checkoutRequest = new
        {
            ShippingAddress = "123 Test St",
            PaymentMethod = "CreditCard"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/cart/checkout", checkoutRequest);

        // Assert
        response.EnsureSuccessStatusCode();
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
