using IntervalToast.Domain.Entities;
using IntervalToast.Domain.Enums;
using IntervalToast.Domain.ValueObjects;

namespace IntervalToast.Application.Interfaces;

/// <summary>
/// Service interface for notification management
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows a notification immediately
    /// </summary>
    Task<Notification> ShowNotificationAsync(string title, string message,
        NotificationCategory category = NotificationCategory.Info,
        NotificationPriority priority = NotificationPriority.Normal,
        NotificationStyle? style = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a notification for future display
    /// </summary>
    Task<Notification> ScheduleNotificationAsync(string title, string message,
        DateTimeOffset scheduledAt,
        NotificationCategory category = NotificationCategory.Info,
        NotificationPriority priority = NotificationPriority.Normal,
        NotificationStyle? style = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses a notification by ID
    /// </summary>
    Task<bool> DismissNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses all active notifications
    /// </summary>
    Task DismissAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active notifications
    /// </summary>
    Task<IEnumerable<Notification>> GetActiveNotificationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notification history
    /// </summary>
    Task<IEnumerable<Notification>> GetNotificationHistoryAsync(int maxResults = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old dismissed notifications
    /// </summary>
    Task<int> CleanupOldNotificationsAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets queue status information
    /// </summary>
    Task<NotificationQueueStatus> GetQueueStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a notification is displayed
    /// </summary>
    event EventHandler<NotificationDisplayedEventArgs>? NotificationDisplayed;

    /// <summary>
    /// Event raised when a notification is dismissed
    /// </summary>
    event EventHandler<NotificationDismissedEventArgs>? NotificationDismissed;
}

/// <summary>
/// Event arguments for notification displayed event
/// </summary>
public sealed class NotificationDisplayedEventArgs : EventArgs
{
    public Notification Notification { get; }
    public DateTimeOffset DisplayedAt { get; }

    public NotificationDisplayedEventArgs(Notification notification, DateTimeOffset displayedAt)
    {
        Notification = notification;
        DisplayedAt = displayedAt;
    }
}

/// <summary>
/// Event arguments for notification dismissed event
/// </summary>
public sealed class NotificationDismissedEventArgs : EventArgs
{
    public Notification Notification { get; }
    public DateTimeOffset DismissedAt { get; }
    public bool UserDismissed { get; }

    public NotificationDismissedEventArgs(Notification notification, DateTimeOffset dismissedAt, bool userDismissed)
    {
        Notification = notification;
        DismissedAt = dismissedAt;
        UserDismissed = userDismissed;
    }
}

/// <summary>
/// Notification queue status information
/// </summary>
public sealed record NotificationQueueStatus
{
    public int ActiveCount { get; init; }
    public int PendingCount { get; init; }
    public int TotalCount { get; init; }
    public bool IsSystemEnabled { get; init; }
    public DateTimeOffset LastActivity { get; init; }
}