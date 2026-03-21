using Payments.Domain.Entities;
using SharedKernel.Interfaces;

namespace Payments.Domain.Repositories;

public interface IPaymentRepository : IRepository<Payment, Guid>
{
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}
