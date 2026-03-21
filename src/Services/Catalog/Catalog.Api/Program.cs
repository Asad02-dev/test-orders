using Authentication.Extensions;
using Catalog.Application.Services;
using Catalog.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Observability.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Observability
builder.AddObservability("catalog-api");

// OpenAPI
builder.Services.AddOpenApi();

// Authentication
builder.Services.AddKeycloakAuthentication(builder.Configuration);

// Infrastructure (EF Core, repositories, services)
builder.Services.AddCatalogInfrastructure(builder.Configuration);

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Catalog.Infrastructure.Persistence.CatalogDbContext>("catalog-db");

// ProblemDetails
builder.Services.AddProblemDetails();

var app = builder.Build();

// Ensure schema exists on startup (creates tables + seed data if not present)
await app.Services.EnsureDatabaseCreatedAsync();

app.UseRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

// --- Products endpoints ---
var products = app.MapGroup("/api/products").WithTags("Products");

products.MapGet("/", async (
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromQuery] string? category,
    ProductService productService,
    CancellationToken ct) =>
{
    if (page <= 0) page = 1;
    if (pageSize <= 0 || pageSize > 100) pageSize = 20;
    var result = await productService.GetProductsAsync(page, pageSize, category, ct);
    return Results.Ok(result);
})
.WithName("GetProducts")
.WithSummary("Get paginated list of products");

products.MapGet("/{id:guid}", async (Guid id, ProductService productService, CancellationToken ct) =>
{
    var product = await productService.GetProductByIdAsync(id, ct);
    return product is null ? Results.NotFound() : Results.Ok(product);
})
.WithName("GetProductById")
.WithSummary("Get product by ID");

products.MapPost("/", async (
    [FromBody] Catalog.Application.DTOs.CreateProductRequest request,
    ProductService productService,
    CancellationToken ct) =>
{
    var result = await productService.CreateProductAsync(request, ct);
    if (result.IsFailure)
        return Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    return Results.Created($"/api/products/{result.Value.Id}", result.Value);
})
.WithName("CreateProduct")
.WithSummary("Create a new product")
.RequireAuthorization();

products.MapPut("/{id:guid}", async (
    Guid id,
    [FromBody] Catalog.Application.DTOs.UpdateProductRequest request,
    ProductService productService,
    CancellationToken ct) =>
{
    var result = await productService.UpdateProductAsync(id, request, ct);
    if (result.IsFailure)
        return Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    return Results.Ok(result.Value);
})
.WithName("UpdateProduct")
.WithSummary("Update a product")
.RequireAuthorization();

products.MapDelete("/{id:guid}", async (Guid id, ProductService productService, CancellationToken ct) =>
{
    var result = await productService.DeleteProductAsync(id, ct);
    if (result.IsFailure)
        return Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    return Results.NoContent();
})
.WithName("DeleteProduct")
.WithSummary("Deactivate a product")
.RequireAuthorization();

app.Run();
