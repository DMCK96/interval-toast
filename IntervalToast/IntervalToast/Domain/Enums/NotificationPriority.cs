namespace IntervalToast.Domain.Enums;

/// <summary>
/// Defines priority levels for notification ordering and timeout behavior
/// </summary>
public enum NotificationPriority
{
    /// <summary>Low priority with extended timeout</summary>
    Low = 0,

    /// <summary>Normal priority with standard timeout</summary>
    Normal = 1,

    /// <summary>High priority with reduced timeout</summary>
    High = 2,

    /// <summary>Critical priority requiring immediate attention</summary>
    Critical = 3
}