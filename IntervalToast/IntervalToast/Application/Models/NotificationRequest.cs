using IntervalToast.Domain.Enums;
using IntervalToast.Domain.ValueObjects;

namespace IntervalToast.Application.Models;

/// <summary>
/// Data transfer object for notification creation requests
/// </summary>
public sealed record NotificationRequest
{
    /// <summary>
    /// Gets the notification title
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets the notification message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the notification category
    /// </summary>
    public NotificationCategory Category { get; init; } = NotificationCategory.Info;

    /// <summary>
    /// Gets the priority level
    /// </summary>
    public NotificationPriority Priority { get; init; } = NotificationPriority.Normal;

    /// <summary>
    /// Gets the custom style (null for category default)
    /// </summary>
    public NotificationStyle? Style { get; init; }

    /// <summary>
    /// Gets the scheduled display time (null for immediate)
    /// </summary>
    public DateTimeOffset? ScheduledAt { get; init; }

    /// <summary>
    /// Gets the display duration in milliseconds (null for default)
    /// </summary>
    public int? DisplayDurationMs { get; init; }

    /// <summary>
    /// Gets whether the notification can be dismissed
    /// </summary>
    public bool CanDismiss { get; init; } = true;

    /// <summary>
    /// Validates the request
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Message);

    /// <summary>
    /// Creates an info notification request
    /// </summary>
    public static NotificationRequest CreateInfo(string title, string message)
    {
        return new NotificationRequest
        {
            Title = title,
            Message = message,
            Category = NotificationCategory.Info,
            Priority = NotificationPriority.Normal
        };
    }

    /// <summary>
    /// Creates a success notification request
    /// </summary>
    public static NotificationRequest CreateSuccess(string title, string message)
    {
        return new NotificationRequest
        {
            Title = title,
            Message = message,
            Category = NotificationCategory.Success,
            Priority = NotificationPriority.Normal
        };
    }

    /// <summary>
    /// Creates a warning notification request
    /// </summary>
    public static NotificationRequest CreateWarning(string title, string message)
    {
        return new NotificationRequest
        {
            Title = title,
            Message = message,
            Category = NotificationCategory.Warning,
            Priority = NotificationPriority.High
        };
    }

    /// <summary>
    /// Creates an error notification request
    /// </summary>
    public static NotificationRequest CreateError(string title, string message)
    {
        return new NotificationRequest
        {
            Title = title,
            Message = message,
            Category = NotificationCategory.Error,
            Priority = NotificationPriority.Critical
        };
    }
}