using IntervalToast.Application.Interfaces;
using IntervalToast.Application.Models;
using IntervalToast.Domain.Entities;
using IntervalToast.Domain.Enums;
using IntervalToast.Domain.Interfaces;
using IntervalToast.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Timer = System.Threading.Timer;

namespace IntervalToast.Infrastructure.Services;

/// <summary>
/// Implementation of notification service
/// </summary>
public sealed class NotificationService : INotificationService, IDisposable
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<NotificationService> _logger;
    private readonly ConcurrentDictionary<Guid, Guid> _activeWindows = new();
    private readonly Timer _maintenanceTimer;
    private bool _disposed;

    public NotificationService(
        INotificationRepository repository,
        ILogger<NotificationService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Setup maintenance timer to run every minute
        _maintenanceTimer = new Timer(PerformMaintenance, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        _logger.LogInformation("Notification service initialized");
    }

    /// <inheritdoc />
    public async Task<Notification> ShowNotificationAsync(string title, string message,
        NotificationCategory category = NotificationCategory.Info,
        NotificationPriority priority = NotificationPriority.Normal,
        NotificationStyle? style = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        try
        {
            // Create notification entity
            var notification = new Notification(title, message, category, priority, style);

            // Add to repository
            await _repository.AddAsync(notification, cancellationToken);

            // Track active notification
            _activeWindows[notification.Id] = notification.Id;

            // Mark as displayed
            notification.MarkAsDisplayed();
            await _repository.UpdateAsync(notification, cancellationToken);

            // TODO: Show the window - this will be handled by the presentation layer

            // Raise event
            NotificationDisplayed?.Invoke(this, new NotificationDisplayedEventArgs(notification, DateTimeOffset.UtcNow));

            _logger.LogInformation("Notification displayed: {NotificationId} - {Title}", notification.Id, title);
            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show notification: {Title}", title);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Notification> ScheduleNotificationAsync(string title, string message,
        DateTimeOffset scheduledAt,
        NotificationCategory category = NotificationCategory.Info,
        NotificationPriority priority = NotificationPriority.Normal,
        NotificationStyle? style = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        try
        {
            // Create scheduled notification entity
            var notification = new Notification(title, message, category, priority, style, scheduledAt);

            // Add to repository
            await _repository.AddAsync(notification, cancellationToken);

            _logger.LogInformation("Notification scheduled: {NotificationId} - {Title} at {ScheduledAt}",
                notification.Id, title, scheduledAt);
            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule notification: {Title}", title);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DismissNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get notification from repository
            var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);
            if (notification == null)
            {
                _logger.LogWarning("Attempted to dismiss non-existent notification: {NotificationId}", notificationId);
                return false;
            }

            // Remove from active notifications
            _activeWindows.TryRemove(notificationId, out _);

            // Mark as dismissed
            notification.MarkAsDismissed();
            await _repository.UpdateAsync(notification, cancellationToken);

            // Raise event
            NotificationDismissed?.Invoke(this, new NotificationDismissedEventArgs(notification, DateTimeOffset.UtcNow, true));

            _logger.LogInformation("Notification dismissed: {NotificationId}", notificationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dismiss notification: {NotificationId}", notificationId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task DismissAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var activeNotifications = await GetActiveNotificationsAsync(cancellationToken);

            foreach (var notification in activeNotifications)
            {
                await DismissNotificationAsync(notification.Id, cancellationToken);
            }

            _logger.LogInformation("All notifications dismissed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dismiss all notifications");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Notification>> GetActiveNotificationsAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetActiveAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Notification>> GetNotificationHistoryAsync(int maxResults = 100, CancellationToken cancellationToken = default)
    {
        var allNotifications = await _repository.GetAllAsync(cancellationToken);
        return allNotifications.Take(maxResults);
    }

    /// <inheritdoc />
    public async Task<int> CleanupOldNotificationsAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTimeOffset.UtcNow - olderThan;
        return await _repository.CleanupOldNotificationsAsync(cutoffDate, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<NotificationQueueStatus> GetQueueStatusAsync(CancellationToken cancellationToken = default)
    {
        var allNotifications = await _repository.GetAllAsync(cancellationToken);
        var activeCount = allNotifications.Count(n => n.IsDisplayed && !n.IsDismissed);
        var pendingCount = allNotifications.Count(n => !n.IsDisplayed && !n.IsDismissed);
        var totalCount = allNotifications.Count();
        var lastActivity = allNotifications.Any() ? allNotifications.Max(n => n.CreatedAt) : DateTimeOffset.MinValue;

        return new NotificationQueueStatus
        {
            ActiveCount = activeCount,
            PendingCount = pendingCount,
            TotalCount = totalCount,
            IsSystemEnabled = true,
            LastActivity = lastActivity
        };
    }

    /// <inheritdoc />
    public event EventHandler<NotificationDisplayedEventArgs>? NotificationDisplayed;

    /// <inheritdoc />
    public event EventHandler<NotificationDismissedEventArgs>? NotificationDismissed;

    private async void PerformMaintenance(object? state)
    {
        try
        {
            // Clean up old notifications (older than 7 days)
            var cleanedCount = await CleanupOldNotificationsAsync(TimeSpan.FromDays(7));
            if (cleanedCount > 0)
            {
                _logger.LogDebug("Maintenance: Cleaned up {Count} old notifications", cleanedCount);
            }

            // TODO: Remove closed windows from active windows
            // This will be handled by the presentation layer
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during notification service maintenance");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _maintenanceTimer?.Dispose();

        // Clear all active notifications
        // TODO: The presentation layer should handle closing actual windows

        _activeWindows.Clear();
        _disposed = true;

        _logger.LogInformation("Notification service disposed");
    }
}

