using Cart.Application.DTOs;
using Cart.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using SharedKernel.Common;
using SharedKernel.Interfaces;
using System.Net.Http.Json;

namespace Cart.Application.Services;

public class CartCheckoutService
{
    private readonly ICartRepository _cartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly HttpClient _ordersHttpClient;
    private readonly CorrelationContext _correlationContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartCheckoutService(
        ICartRepository cartRepository,
        IUnitOfWork unitOfWork,
        HttpClient ordersHttpClient,
        CorrelationContext correlationContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
        _ordersHttpClient = ordersHttpClient;
        _correlationContext = correlationContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<CartCheckoutResponse>> CheckoutAsync(
        Guid customerId,
        CartCheckoutRequest request,
        CancellationToken ct)
    {
        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, ct);
        if (cart is null || !cart.Items.Any())
            return Result.Failure<CartCheckoutResponse>("Cart is empty or not found.");

        var placeOrderRequest = new
        {
            CustomerId = customerId,
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName,
            IdempotencyKey = request.IdempotencyKey,
            Items = cart.Items.Select(i => new
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList()
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/orders");
        requestMessage.Content = JsonContent.Create(placeOrderRequest);
        requestMessage.Headers.Add("X-Correlation-Id", _correlationContext.CorrelationId.ToString());

        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authHeader))
            requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader);

        var response = await _ordersHttpClient.SendAsync(requestMessage, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return Result.Failure<CartCheckoutResponse>($"Order placement failed (HTTP {(int)response.StatusCode}): {errorBody}");
        }

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>(ct);
        if (order is null)
            return Result.Failure<CartCheckoutResponse>("Invalid response from Orders service.");

        cart.Clear();
        _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new CartCheckoutResponse(order.Id, order.StatusText, order.TotalAmount));
    }

    private sealed record OrderResponse(Guid Id, string StatusText, decimal TotalAmount);
}
