using Notifications.Application.Models;

namespace Notifications.Application.Repositories;

public interface INotificationRepository
{
    Task AddAsync(NotificationLog log, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationLog>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationLog>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
