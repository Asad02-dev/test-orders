using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Catalog.Application.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Catalog.Api.Tests;

public class ProductsEndpointsTests : IClassFixture<CatalogApiFactory>
{
    private readonly HttpClient _client;
    private readonly CatalogApiFactory _factory;

    public ProductsEndpointsTests(CatalogApiFactory factory)
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
    public async Task GetProducts_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/products?page=1&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetProducts_WithDefaultPagination_ReturnsResults()
    {
        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetProductById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/products/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsCreated()
    {
        // Arrange
        var request = new CreateProductRequest(
            Name: "Test Product",
            Description: "Test Description",
            Price: 99.99m,
            Sku: "TEST-SKU-" + Guid.NewGuid(),
            Category: "Electronics",
            ImageUrl: "https://example.com/image.jpg",
            Currency: "USD"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Price, result.Price);
        Assert.Equal(request.Sku, result.Sku);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateProduct_WithDuplicateSku_ReturnsBadRequest()
    {
        // Arrange
        var sku = "DUPLICATE-SKU-" + Guid.NewGuid();
        var request1 = new CreateProductRequest(
            Name: "Product 1",
            Description: "Description 1",
            Price: 50.00m,
            Sku: sku,
            Category: "Test",
            ImageUrl: "",
            Currency: "USD"
        );
        var request2 = new CreateProductRequest(
            Name: "Product 2",
            Description: "Description 2",
            Price: 60.00m,
            Sku: sku,
            Category: "Test",
            ImageUrl: "",
            Currency: "USD"
        );

        // Act
        await _client.PostAsJsonAsync("/api/products", request1);
        var response = await _client.PostAsJsonAsync("/api/products", request2);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_WithValidData_ReturnsOk()
    {
        // Arrange - Create a product first
        var createRequest = new CreateProductRequest(
            Name: "Original Product",
            Description: "Original Description",
            Price: 100.00m,
            Sku: "UPDATE-TEST-" + Guid.NewGuid(),
            Category: "Original Category",
            ImageUrl: "",
            Currency: "USD"
        );
        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        var updateRequest = new UpdateProductRequest(
            Name: "Updated Product",
            Description: "Updated Description",
            Price: 150.00m,
            Category: "Updated Category",
            ImageUrl: "https://example.com/updated.jpg"
        );

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{createdProduct!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(result);
        Assert.Equal(updateRequest.Name, result.Name);
        Assert.Equal(updateRequest.Price, result.Price);
    }

    [Fact]
    public async Task UpdateProduct_WithNonExistentId_ReturnsBadRequest()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateProductRequest(
            Name: "Updated Product",
            Description: "Updated Description",
            Price: 150.00m,
            Category: "Updated Category",
            ImageUrl: ""
        );

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{nonExistentId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_WithValidId_ReturnsNoContent()
    {
        // Arrange - Create a product first
        var createRequest = new CreateProductRequest(
            Name: "Product to Delete",
            Description: "Will be deleted",
            Price: 50.00m,
            Sku: "DELETE-TEST-" + Guid.NewGuid(),
            Category: "Test",
            ImageUrl: "",
            Currency: "USD"
        );
        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/products/{createdProduct!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify product is deactivated (not actually deleted)
        var getResponse = await _client.GetAsync($"/api/products/{createdProduct.Id}");
        if (getResponse.IsSuccessStatusCode)
        {
            var product = await getResponse.Content.ReadFromJsonAsync<ProductDto>();
            Assert.NotNull(product);
            Assert.False(product.IsActive);
        }
    }

    [Fact]
    public async Task DeleteProduct_WithNonExistentId_ReturnsBadRequest()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/products/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithCategoryFilter_ReturnsFilteredResults()
    {
        // Arrange - Create products in different categories
        var category = "FilterTest-" + Guid.NewGuid();
        var request = new CreateProductRequest(
            Name: "Filtered Product",
            Description: "Test",
            Price: 100.00m,
            Sku: "FILTER-" + Guid.NewGuid(),
            Category: category,
            ImageUrl: "",
            Currency: "USD"
        );
        await _client.PostAsJsonAsync("/api/products", request);

        // Act
        var response = await _client.GetAsync($"/api/products?category={category}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        Assert.NotNull(result);
        Assert.All(result.Items, p => Assert.Equal(category, p.Category));
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
