using Microsoft.EntityFrameworkCore;
using Payments.Domain.Entities;
using Payments.Domain.Repositories;

namespace Payments.Infrastructure.Persistence;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentsDbContext _context;
    public PaymentRepository(PaymentsDbContext context) { _context = context; }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Payments.FindAsync([id], ct);
    public async Task AddAsync(Payment entity, CancellationToken ct = default)
        => await _context.Payments.AddAsync(entity, ct);
    public void Update(Payment entity) => _context.Payments.Update(entity);
    public void Remove(Payment entity) => _context.Payments.Remove(entity);
    public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        => await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId, ct);
    public async Task<Payment?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default)
        => await _context.Payments.FirstOrDefaultAsync(p => p.IdempotencyKey == key, ct);
}
