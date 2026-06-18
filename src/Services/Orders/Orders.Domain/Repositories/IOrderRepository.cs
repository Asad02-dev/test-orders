using Orders.Domain.Entities;
using SharedKernel.Interfaces;

namespace Orders.Domain.Repositories;

public interface IOrderRepository : IRepository<Order, Guid>
{
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, Guid? customerId = null, CancellationToken cancellationToken = default);
}
