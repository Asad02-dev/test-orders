using Cart.Application.DTOs;
using Cart.Domain.Repositories;
using SharedKernel.Common;
using SharedKernel.Interfaces;

namespace Cart.Application.Services;

public class CartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CartService(ICartRepository cartRepository, IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CartDto?> GetCartAsync(Guid customerId, CancellationToken ct)
    {
        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, ct);
        return cart is null ? null : MapToDto(cart);
    }

    public async Task<CartDto> GetOrCreateCartAsync(Guid customerId, CancellationToken ct)
    {
        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, ct);
        if (cart is null)
        {
            cart = Cart.Domain.Entities.Cart.Create(customerId);
            await _cartRepository.AddAsync(cart, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        return MapToDto(cart);
    }

    public async Task<Result<CartDto>> AddItemAsync(Guid customerId, AddToCartRequest request, CancellationToken ct)
    {
        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, ct);
        var isNew = cart is null;
        if (isNew)
        {
            cart = Cart.Domain.Entities.Cart.Create(customerId);
            await _cartRepository.AddAsync(cart, ct);
        }

        cart.AddItem(request.ProductId, request.ProductName, request.UnitPrice, request.Quantity);
        if (!isNew)
            _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(MapToDto(cart));
    }

    public async Task<Result<CartDto>> UpdateItemQuantityAsync(
        Guid customerId, UpdateCartItemRequest request, CancellationToken ct)
    {
        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, ct);
        if (cart is null)
            return Result.Failure<CartDto>("Cart not found.");

        cart.UpdateItemQuantity(request.ProductId, request.Quantity);
        _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(MapToDto(cart));
    }

    public async Task<Result> RemoveItemAsync(Guid customerId, Guid productId, CancellationToken ct)
    {
        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, ct);
        if (cart is null)
            return Result.Failure("Cart not found.");

        cart.RemoveItem(productId);
        _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> ClearCartAsync(Guid customerId, CancellationToken ct)
    {
        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, ct);
        if (cart is null)
            return Result.Failure("Cart not found.");

        cart.Clear();
        _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    private static CartDto MapToDto(Cart.Domain.Entities.Cart cart) => new(
        cart.Id,
        cart.CustomerId,
        cart.Items.Select(i => new CartItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.TotalPrice)).ToList(),
        cart.TotalAmount,
        cart.UpdatedAt
    );
}
