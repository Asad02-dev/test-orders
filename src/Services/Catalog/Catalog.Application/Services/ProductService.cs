using Catalog.Application.DTOs;
using Catalog.Domain.Entities;
using Catalog.Domain.Repositories;
using SharedKernel.Common;
using SharedKernel.Interfaces;

namespace Catalog.Application.Services;

public class ProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(
        int page, int pageSize, string? category, CancellationToken ct)
    {
        var (items, totalCount) = await _productRepository.GetPagedAsync(page, pageSize, category, ct);
        return new PagedResult<ProductDto>(
            items.Select(MapToDto).ToList(),
            totalCount, page, pageSize);
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken ct)
    {
        var product = await _productRepository.GetByIdAsync(id, ct);
        return product is null ? null : MapToDto(product);
    }

    public async Task<Result<ProductDto>> CreateProductAsync(CreateProductRequest request, CancellationToken ct)
    {
        var existing = await _productRepository.GetBySkuAsync(request.Sku, ct);
        if (existing is not null)
            return Result.Failure<ProductDto>($"Product with SKU '{request.Sku}' already exists.");

        var product = Product.Create(
            request.Name,
            request.Description,
            request.Price,
            request.Sku,
            request.Category,
            request.ImageUrl,
            request.Currency);

        await _productRepository.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(MapToDto(product));
    }

    public async Task<Result<ProductDto>> UpdateProductAsync(
        Guid id, UpdateProductRequest request, CancellationToken ct)
    {
        var product = await _productRepository.GetByIdAsync(id, ct);
        if (product is null)
            return Result.Failure<ProductDto>($"Product '{id}' not found.");

        product.UpdateDetails(request.Name, request.Description, request.Price, request.Category, request.ImageUrl);
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(MapToDto(product));
    }

    public async Task<Result> DeleteProductAsync(Guid id, CancellationToken ct)
    {
        var product = await _productRepository.GetByIdAsync(id, ct);
        if (product is null)
            return Result.Failure($"Product '{id}' not found.");

        product.Deactivate();
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    private static ProductDto MapToDto(Product p) => new(
        p.Id, p.Name, p.Description, p.Price, p.Currency,
        p.Sku, p.Category, p.ImageUrl, p.IsActive, p.CreatedAt, p.UpdatedAt);
}
