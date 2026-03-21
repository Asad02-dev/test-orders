using Cart.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Cart.Infrastructure.Persistence;

public class CartRepository : ICartRepository
{
    private readonly CartDbContext _context;

    public CartRepository(CartDbContext context)
    {
        _context = context;
    }

    public async Task<Cart.Domain.Entities.Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(Cart.Domain.Entities.Cart entity, CancellationToken cancellationToken = default)
        => await _context.Carts.AddAsync(entity, cancellationToken);

    public void Update(Cart.Domain.Entities.Cart entity)
        => _context.Carts.Update(entity);

    public void Remove(Cart.Domain.Entities.Cart entity)
        => _context.Carts.Remove(entity);

    public async Task<Cart.Domain.Entities.Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        => await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
}
