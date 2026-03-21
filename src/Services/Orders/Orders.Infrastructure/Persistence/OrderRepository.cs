using Microsoft.EntityFrameworkCore;
using Orders.Domain.Entities;
using Orders.Domain.Repositories;

namespace Orders.Infrastructure.Persistence;

public class OrderRepository : IOrderRepository
{
    private readonly OrdersDbContext _context;

    public OrderRepository(OrdersDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task AddAsync(Order entity, CancellationToken ct = default)
        => await _context.Orders.AddAsync(entity, ct);

    public void Update(Order entity) => _context.Orders.Update(entity);
    public void Remove(Order entity) => _context.Orders.Remove(entity);

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
        => await _context.Orders.Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task<Order?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default)
        => await _context.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.IdempotencyKey == key, ct);

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, Guid? customerId = null, CancellationToken ct = default)
    {
        var query = _context.Orders.Include(o => o.Items).AsQueryable();
        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, totalCount);
    }
}
