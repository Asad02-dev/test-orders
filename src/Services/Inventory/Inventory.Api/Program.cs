using Authentication.Extensions;
using Inventory.Application.DTOs;
using Inventory.Application.Services;
using Inventory.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Observability.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("inventory-api");
builder.Services.AddOpenApi();
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddInventoryInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Inventory.Infrastructure.Persistence.InventoryDbContext>("inventory-db");
builder.Services.AddProblemDetails();

var app = builder.Build();

// Ensure schema exists on startup
await app.Services.EnsureDatabaseCreatedAsync();

app.UseRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

var inventoryGroup = app.MapGroup("/api/inventory").WithTags("Inventory");

inventoryGroup.MapGet("/products/{productId:guid}", async (Guid productId, InventoryService svc, CancellationToken ct) =>
{
    var item = await svc.GetByProductIdAsync(productId, ct);
    return item is null ? Results.NotFound() : Results.Ok(item);
}).WithName("GetInventoryByProductId");

inventoryGroup.MapGet("/low-stock", async (InventoryService svc, CancellationToken ct) =>
    Results.Ok(await svc.GetLowStockAsync(ct)))
.WithName("GetLowStockItems").RequireAuthorization();

inventoryGroup.MapPost("/", async ([FromBody] CreateInventoryItemRequest request, InventoryService svc, CancellationToken ct) =>
{
    var result = await svc.CreateAsync(request, ct);
    if (result.IsFailure) return Results.Problem(result.Error, statusCode: 400);
    return Results.Created($"/api/inventory/products/{result.Value.ProductId}", result.Value);
}).WithName("CreateInventoryItem").RequireAuthorization();

inventoryGroup.MapPost("/products/{productId:guid}/restock", async (Guid productId, [FromBody] RestockRequest request, InventoryService svc, CancellationToken ct) =>
{
    var result = await svc.RestockAsync(productId, request, ct);
    if (result.IsFailure) return Results.Problem(result.Error, statusCode: 400);
    return Results.Ok(result.Value);
}).WithName("RestockProduct").RequireAuthorization();

app.Run();
