using Cart.Application.DTOs;
using Cart.Application.Services;
using Cart.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Observability.Extensions;
using System.Security.Claims;
using Authentication.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("cart-api");
builder.Services.AddOpenApi();
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddCartInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Cart.Infrastructure.Persistence.CartDbContext>("cart-db");
builder.Services.AddProblemDetails();

var app = builder.Build();

// Ensure schema exists on startup
await app.Services.EnsureDatabaseCreatedAsync();

app.UseRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

var cartGroup = app.MapGroup("/api/cart").WithTags("Cart").RequireAuthorization();

cartGroup.MapGet("/", async (ClaimsPrincipal user, CartService cartService, CancellationToken ct) =>
{
    var customerId = GetCustomerId(user);
    var cart = await cartService.GetOrCreateCartAsync(customerId, ct);
    return Results.Ok(cart);
})
.WithName("GetCart")
.WithSummary("Get the current user's cart");

cartGroup.MapPost("/items", async (
    [FromBody] AddToCartRequest request,
    ClaimsPrincipal user,
    CartService cartService,
    CancellationToken ct) =>
{
    var customerId = GetCustomerId(user);
    var result = await cartService.AddItemAsync(customerId, request, ct);
    if (result.IsFailure) return Results.Problem(result.Error, statusCode: 400);
    return Results.Ok(result.Value);
})
.WithName("AddItemToCart")
.WithSummary("Add an item to the cart");

cartGroup.MapPut("/items", async (
    [FromBody] UpdateCartItemRequest request,
    ClaimsPrincipal user,
    CartService cartService,
    CancellationToken ct) =>
{
    var customerId = GetCustomerId(user);
    var result = await cartService.UpdateItemQuantityAsync(customerId, request, ct);
    if (result.IsFailure) return Results.Problem(result.Error, statusCode: 400);
    return Results.Ok(result.Value);
})
.WithName("UpdateCartItem")
.WithSummary("Update quantity of an item in the cart");

cartGroup.MapDelete("/items/{productId:guid}", async (
    Guid productId,
    ClaimsPrincipal user,
    CartService cartService,
    CancellationToken ct) =>
{
    var customerId = GetCustomerId(user);
    var result = await cartService.RemoveItemAsync(customerId, productId, ct);
    if (result.IsFailure) return Results.Problem(result.Error, statusCode: 400);
    return Results.NoContent();
})
.WithName("RemoveCartItem")
.WithSummary("Remove an item from the cart");

cartGroup.MapDelete("/", async (ClaimsPrincipal user, CartService cartService, CancellationToken ct) =>
{
    var customerId = GetCustomerId(user);
    var result = await cartService.ClearCartAsync(customerId, ct);
    if (result.IsFailure) return Results.Problem(result.Error, statusCode: 400);
    return Results.NoContent();
})
.WithName("ClearCart")
.WithSummary("Clear all items from the cart");

app.Run();

static Guid GetCustomerId(ClaimsPrincipal user)
{
    var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User ID claim not found.");
    return Guid.Parse(sub);
}
