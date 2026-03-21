using Catalog.Domain.Entities;
using Catalog.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _context;

    public ProductRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Products.FindAsync([id], cancellationToken);

    public async Task AddAsync(Product entity, CancellationToken cancellationToken = default)
        => await _context.Products.AddAsync(entity, cancellationToken);

    public void Update(Product entity)
        => _context.Products.Update(entity);

    public void Remove(Product entity)
        => _context.Products.Remove(entity);

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Products.Where(p => p.IsActive).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
        => await _context.Products.Where(p => p.IsActive && p.Category == category).ToListAsync(cancellationToken);

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
        => await _context.Products.FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? category = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
