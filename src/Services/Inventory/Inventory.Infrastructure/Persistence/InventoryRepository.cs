using Inventory.Domain.Entities;
using Inventory.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public class InventoryRepository : IInventoryRepository
{
    private readonly InventoryDbContext _context;
    public InventoryRepository(InventoryDbContext context) { _context = context; }

    public async Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.InventoryItems.FindAsync([id], ct);
    public async Task AddAsync(InventoryItem entity, CancellationToken ct = default)
        => await _context.InventoryItems.AddAsync(entity, ct);
    public void Update(InventoryItem entity) => _context.InventoryItems.Update(entity);
    public void Remove(InventoryItem entity) => _context.InventoryItems.Remove(entity);
    public async Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId, ct);
    public async Task<IReadOnlyList<InventoryItem>> GetByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken ct = default)
        => await _context.InventoryItems.Where(i => productIds.Contains(i.ProductId)).ToListAsync(ct);
    public async Task<IReadOnlyList<InventoryItem>> GetLowStockItemsAsync(CancellationToken ct = default)
        => await _context.InventoryItems.Where(i => i.AvailableQuantity <= i.ReorderThreshold).ToListAsync(ct);
}
