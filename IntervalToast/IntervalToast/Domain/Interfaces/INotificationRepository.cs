using IntervalToast.Domain.Entities;

namespace IntervalToast.Domain.Interfaces;

/// <summary>
/// Repository interface for notification persistence
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Adds a notification to the repository
    /// </summary>
    Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a notification by ID
    /// </summary>
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all notifications
    /// </summary>
    Task<IEnumerable<Notification>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications that should be displayed now
    /// </summary>
    Task<IEnumerable<Notification>> GetScheduledForNowAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active (displayed but not dismissed) notifications
    /// </summary>
    Task<IEnumerable<Notification>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a notification
    /// </summary>
    Task<Notification> UpdateAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a notification
    /// </summary>
    Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all dismissed notifications older than the specified date
    /// </summary>
    Task<int> CleanupOldNotificationsAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default);
}