using IntervalToast.Domain.Entities;
using IntervalToast.Domain.Interfaces;
using System.Collections.Concurrent;

namespace IntervalToast.Infrastructure.Persistence;

/// <summary>
/// In-memory implementation of notification repository for runtime storage
/// </summary>
public sealed class InMemoryNotificationRepository : INotificationRepository
{
    private readonly ConcurrentDictionary<Guid, Notification> _notifications = new();

    /// <inheritdoc />
    public Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (!_notifications.TryAdd(notification.Id, notification))
            throw new InvalidOperationException($"Notification with ID {notification.Id} already exists");

        return Task.FromResult(notification);
    }

    /// <inheritdoc />
    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _notifications.TryGetValue(id, out var notification);
        return Task.FromResult(notification);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Notification>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var notifications = _notifications.Values.OrderByDescending(n => n.CreatedAt).AsEnumerable();
        return Task.FromResult(notifications);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Notification>> GetScheduledForNowAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var notifications = _notifications.Values
            .Where(n => n.ShouldDisplayNow())
            .OrderBy(n => n.Priority)
            .ThenBy(n => n.CreatedAt)
            .AsEnumerable();

        return Task.FromResult(notifications);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Notification>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var notifications = _notifications.Values
            .Where(n => n.IsDisplayed && !n.IsDismissed)
            .OrderByDescending(n => n.DisplayedAt)
            .AsEnumerable();

        return Task.FromResult(notifications);
    }

    /// <inheritdoc />
    public Task<Notification> UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (!_notifications.ContainsKey(notification.Id))
            throw new InvalidOperationException($"Notification with ID {notification.Id} not found");

        _notifications[notification.Id] = notification;
        return Task.FromResult(notification);
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var removed = _notifications.TryRemove(id, out _);
        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public Task<int> CleanupOldNotificationsAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default)
    {
        var oldNotifications = _notifications.Values
            .Where(n => n.IsDismissed && n.DismissedAt < olderThan)
            .ToList();

        var cleanedCount = 0;
        foreach (var notification in oldNotifications)
        {
            if (_notifications.TryRemove(notification.Id, out _))
                cleanedCount++;
        }

        return Task.FromResult(cleanedCount);
    }
}