namespace IntervalToast.Domain.Enums;

/// <summary>
/// Defines notification categories for visual grouping and management
/// </summary>
public enum NotificationCategory
{
    /// <summary>General informational messages</summary>
    Info,

    /// <summary>Success confirmations and positive feedback</summary>
    Success,

    /// <summary>Warning messages requiring attention</summary>
    Warning,

    /// <summary>Error messages indicating problems</summary>
    Error,

    /// <summary>System status and updates</summary>
    System,

    /// <summary>Custom category for specific use cases</summary>
    Custom
}