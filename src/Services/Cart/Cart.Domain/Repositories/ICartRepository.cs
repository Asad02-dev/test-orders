using SharedKernel.Interfaces;

namespace Cart.Domain.Repositories;

public interface ICartRepository : IRepository<Cart.Domain.Entities.Cart, Guid>
{
    Task<Cart.Domain.Entities.Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
}
