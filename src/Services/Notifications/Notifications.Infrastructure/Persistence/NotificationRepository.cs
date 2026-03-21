using Microsoft.EntityFrameworkCore;
using Notifications.Application.Models;
using Notifications.Application.Repositories;

namespace Notifications.Infrastructure.Persistence;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationsDbContext _context;

    public NotificationRepository(NotificationsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(NotificationLog log, CancellationToken cancellationToken = default)
        => await _context.NotificationLogs.AddAsync(log, cancellationToken);

    public async Task<IReadOnlyList<NotificationLog>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        => await _context.NotificationLogs
            .Where(n => n.OrderId == orderId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NotificationLog>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
        => await _context.NotificationLogs
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
