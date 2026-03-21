using Catalog.Domain.Entities;
using SharedKernel.Interfaces;

namespace Catalog.Domain.Repositories;

public interface IProductRepository : IRepository<Product, Guid>
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? category = null, CancellationToken cancellationToken = default);
}
