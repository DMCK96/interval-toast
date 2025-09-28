using IntervalToast.Domain.Enums;
using IntervalToast.Domain.ValueObjects;

namespace IntervalToast.Domain.Entities;

/// <summary>
/// Domain entity representing a notification
/// </summary>
public sealed class Notification
{
    /// <summary>
    /// Gets the unique identifier for this notification
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the title of the notification
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Gets the message content
    /// </summary>
    public string Message { get; private set; }

    /// <summary>
    /// Gets the category of the notification
    /// </summary>
    public NotificationCategory Category { get; private set; }

    /// <summary>
    /// Gets the priority level
    /// </summary>
    public NotificationPriority Priority { get; private set; }

    /// <summary>
    /// Gets the visual styling for this notification
    /// </summary>
    public NotificationStyle Style { get; private set; }

    /// <summary>
    /// Gets the creation timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the scheduled display time (null for immediate display)
    /// </summary>
    public DateTimeOffset? ScheduledAt { get; private set; }

    /// <summary>
    /// Gets the display duration in milliseconds (null for default)
    /// </summary>
    public int? DisplayDurationMs { get; private set; }

    /// <summary>
    /// Gets whether this notification can be dismissed by user action
    /// </summary>
    public bool CanDismiss { get; private set; }

    /// <summary>
    /// Gets whether this notification has been displayed
    /// </summary>
    public bool IsDisplayed { get; private set; }

    /// <summary>
    /// Gets whether this notification has been dismissed
    /// </summary>
    public bool IsDismissed { get; private set; }

    /// <summary>
    /// Gets the time when the notification was displayed
    /// </summary>
    public DateTimeOffset? DisplayedAt { get; private set; }

    /// <summary>
    /// Gets the time when the notification was dismissed
    /// </summary>
    public DateTimeOffset? DismissedAt { get; private set; }

    /// <summary>
    /// Creates a new notification
    /// </summary>
    /// <param name="title">The notification title</param>
    /// <param name="message">The notification message</param>
    /// <param name="category">The notification category</param>
    /// <param name="priority">The priority level</param>
    /// <param name="style">The visual styling (null for default)</param>
    /// <param name="scheduledAt">When to display the notification (null for immediate)</param>
    /// <param name="displayDurationMs">How long to display in milliseconds (null for default)</param>
    /// <param name="canDismiss">Whether the notification can be dismissed</param>
    public Notification(
        string title,
        string message,
        NotificationCategory category = NotificationCategory.Info,
        NotificationPriority priority = NotificationPriority.Normal,
        NotificationStyle? style = null,
        DateTimeOffset? scheduledAt = null,
        int? displayDurationMs = null,
        bool canDismiss = true)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or empty", nameof(title));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or empty", nameof(message));

        Id = Guid.NewGuid();
        Title = title;
        Message = message;
        Category = category;
        Priority = priority;
        Style = style ?? NotificationStyle.ForCategory(category);
        CreatedAt = DateTimeOffset.UtcNow;
        ScheduledAt = scheduledAt;
        DisplayDurationMs = displayDurationMs;
        CanDismiss = canDismiss;
    }

    /// <summary>
    /// Updates the notification content
    /// </summary>
    public void UpdateContent(string title, string message)
    {
        if (IsDisplayed)
            throw new InvalidOperationException("Cannot update content of already displayed notification");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or empty", nameof(title));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or empty", nameof(message));

        Title = title;
        Message = message;
    }

    /// <summary>
    /// Updates the notification style
    /// </summary>
    public void UpdateStyle(NotificationStyle style)
    {
        if (IsDisplayed)
            throw new InvalidOperationException("Cannot update style of already displayed notification");

        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>
    /// Updates the scheduled display time
    /// </summary>
    public void UpdateSchedule(DateTimeOffset? scheduledAt)
    {
        if (IsDisplayed)
            throw new InvalidOperationException("Cannot reschedule already displayed notification");

        ScheduledAt = scheduledAt;
    }

    /// <summary>
    /// Marks the notification as displayed
    /// </summary>
    public void MarkAsDisplayed()
    {
        if (IsDisplayed)
            throw new InvalidOperationException("Notification is already marked as displayed");

        IsDisplayed = true;
        DisplayedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the notification as dismissed
    /// </summary>
    public void MarkAsDismissed()
    {
        if (!CanDismiss)
            throw new InvalidOperationException("This notification cannot be dismissed");

        if (IsDismissed)
            throw new InvalidOperationException("Notification is already dismissed");

        IsDismissed = true;
        DismissedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets whether this notification should be displayed now
    /// </summary>
    public bool ShouldDisplayNow()
    {
        if (IsDisplayed || IsDismissed)
            return false;

        return ScheduledAt == null || ScheduledAt <= DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates an error notification
    /// </summary>
    public static Notification CreateError(string title, string message, bool canDismiss = true)
    {
        return new Notification(title, message, NotificationCategory.Error,
            NotificationPriority.High, canDismiss: canDismiss);
    }

    /// <summary>
    /// Creates a success notification
    /// </summary>
    public static Notification CreateSuccess(string title, string message, bool canDismiss = true)
    {
        return new Notification(title, message, NotificationCategory.Success,
            NotificationPriority.Normal, canDismiss: canDismiss);
    }

    /// <summary>
    /// Creates a warning notification
    /// </summary>
    public static Notification CreateWarning(string title, string message, bool canDismiss = true)
    {
        return new Notification(title, message, NotificationCategory.Warning,
            NotificationPriority.High, canDismiss: canDismiss);
    }

    /// <summary>
    /// Creates an info notification
    /// </summary>
    public static Notification CreateInfo(string title, string message, bool canDismiss = true)
    {
        return new Notification(title, message, NotificationCategory.Info,
            NotificationPriority.Normal, canDismiss: canDismiss);
    }
}