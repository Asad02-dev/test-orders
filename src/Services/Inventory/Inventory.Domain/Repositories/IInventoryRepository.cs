using Inventory.Domain.Entities;
using SharedKernel.Interfaces;

namespace Inventory.Domain.Repositories;

public interface IInventoryRepository : IRepository<InventoryItem, Guid>
{
    Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItem>> GetByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItem>> GetLowStockItemsAsync(CancellationToken cancellationToken = default);
}
