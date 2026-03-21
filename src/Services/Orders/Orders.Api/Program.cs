using Authentication.Extensions;
using Microsoft.AspNetCore.Mvc;
using Observability.Extensions;
using Orders.Application.DTOs;
using Orders.Application.Services;
using Orders.Infrastructure.Extensions;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("orders-api");
builder.Services.AddOpenApi();
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddOrdersInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Orders.Infrastructure.Persistence.OrdersDbContext>("orders-db");
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

var ordersGroup = app.MapGroup("/api/orders").WithTags("Orders").RequireAuthorization();

ordersGroup.MapGet("/", async (
    [FromQuery] int page,
    [FromQuery] int pageSize,
    OrderService orderService,
    ClaimsPrincipal user,
    CancellationToken ct) =>
{
    if (page <= 0) page = 1;
    if (pageSize <= 0 || pageSize > 100) pageSize = 20;
    var customerId = GetCustomerId(user);
    var result = await orderService.GetOrdersAsync(page, pageSize, customerId, ct);
    return Results.Ok(result);
})
.WithName("GetOrders")
.WithSummary("Get paginated list of orders for current user");

ordersGroup.MapGet("/{id:guid}", async (
    Guid id,
    OrderService orderService,
    CancellationToken ct) =>
{
    var order = await orderService.GetOrderByIdAsync(id, ct);
    return order is null ? Results.NotFound() : Results.Ok(order);
})
.WithName("GetOrderById")
.WithSummary("Get order by ID");

ordersGroup.MapPost("/", async (
    [FromBody] PlaceOrderRequest request,
    ClaimsPrincipal user,
    OrderService orderService,
    CancellationToken ct) =>
{
    var customerId = GetCustomerId(user);
    var result = await orderService.PlaceOrderAsync(customerId, request, ct);
    if (result.IsFailure) return Results.Problem(result.Error, statusCode: 400);
    return Results.Created($"/api/orders/{result.Value.Id}", result.Value);
})
.WithName("PlaceOrder")
.WithSummary("Place a new order");

ordersGroup.MapPost("/{id:guid}/cancel", async (
    Guid id,
    [FromBody] CancelOrderRequest request,
    OrderService orderService,
    CancellationToken ct) =>
{
    var result = await orderService.CancelOrderAsync(id, request.Reason, ct);
    if (result.IsFailure) return Results.Problem(result.Error, statusCode: 400);
    return Results.NoContent();
})
.WithName("CancelOrder")
.WithSummary("Cancel an order");

app.Run();

static Guid GetCustomerId(ClaimsPrincipal user)
{
    var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User ID claim not found.");
    return Guid.Parse(sub);
}

public record CancelOrderRequest(string Reason);
