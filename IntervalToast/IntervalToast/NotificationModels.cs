using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace IntervalToast
{
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

    /// <summary>
    /// Defines priority levels for notification ordering and timeout behavior
    /// </summary>
    public enum NotificationPriority
    {
        /// <summary>Low priority - appears at bottom of stack, longer timeout</summary>
        Low = 1,
        /// <summary>Normal priority - standard behavior</summary>
        Normal = 2,
        /// <summary>High priority - appears higher in stack, shorter timeout</summary>
        High = 3,
        /// <summary>Critical priority - top of stack, manual dismiss or extended timeout</summary>
        Critical = 4
    }

    /// <summary>
    /// Complete notification data model containing all notification information
    /// </summary>
    public class NotificationData
    {
        /// <summary>
        /// Unique identifier for this notification
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The notification title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The notification message content
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The notification category
        /// </summary>
        public NotificationCategory Category { get; set; } = NotificationCategory.Info;

        /// <summary>
        /// The notification priority level
        /// </summary>
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        /// <summary>
        /// Optional custom icon content (Unicode symbol or path)
        /// </summary>
        public string? IconContent { get; set; }

        /// <summary>
        /// When the notification was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Custom auto-close delay override (null uses category/priority defaults)
        /// </summary>
        public TimeSpan? CustomTimeout { get; set; }

        /// <summary>
        /// Additional metadata for custom processing
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Creates a basic info notification
        /// </summary>
        public static NotificationData CreateInfo(string title, string message) => new()
        {
            Title = title,
            Message = message,
            Category = NotificationCategory.Info,
            Priority = NotificationPriority.Normal
        };

        /// <summary>
        /// Creates a success notification
        /// </summary>
        public static NotificationData CreateSuccess(string title, string message) => new()
        {
            Title = title,
            Message = message,
            Category = NotificationCategory.Success,
            Priority = NotificationPriority.Normal
        };

        /// <summary>
        /// Creates a warning notification
        /// </summary>
        public static NotificationData CreateWarning(string title, string message) => new()
        {
            Title = title,
            Message = message,
            Category = NotificationCategory.Warning,
            Priority = NotificationPriority.High
        };

        /// <summary>
        /// Creates an error notification
        /// </summary>
        public static NotificationData CreateError(string title, string message) => new()
        {
            Title = title,
            Message = message,
            Category = NotificationCategory.Error,
            Priority = NotificationPriority.Critical
        };

        /// <summary>
        /// Creates a system notification
        /// </summary>
        public static NotificationData CreateSystem(string title, string message) => new()
        {
            Title = title,
            Message = message,
            Category = NotificationCategory.System,
            Priority = NotificationPriority.Low
        };
    }

    /// <summary>
    /// Configuration settings for notification categories
    /// </summary>
    public class CategoryConfiguration
    {
        /// <summary>
        /// Background color for this category
        /// </summary>
        public System.Windows.Media.Color BackgroundColor { get; set; }

        /// <summary>
        /// Accent color for borders and highlights
        /// </summary>
        public System.Windows.Media.Color AccentColor { get; set; }

        /// <summary>
        /// Icon content (Unicode symbol or path)
        /// </summary>
        public string IconContent { get; set; } = string.Empty;

        /// <summary>
        /// Default timeout for this category
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Whether this category supports auto-dismiss
        /// </summary>
        public bool AllowAutoDismiss { get; set; } = true;

        /// <summary>
        /// Sound to play for this category (optional)
        /// </summary>
        public string? SoundPath { get; set; }

        /// <summary>
        /// Whether to show this category in overflow summaries
        /// </summary>
        public bool ShowInSummary { get; set; } = true;

        /// <summary>
        /// Gets default category configurations
        /// </summary>
        public static Dictionary<NotificationCategory, CategoryConfiguration> GetDefaults() => new()
        {
            [NotificationCategory.Info] = new()
            {
                BackgroundColor = System.Windows.Media.Color.FromRgb(240, 248, 255),
                AccentColor = System.Windows.Media.Color.FromRgb(74, 144, 226),
                IconContent = "ℹ️",
                DefaultTimeout = TimeSpan.FromSeconds(5),
                AllowAutoDismiss = true
            },
            [NotificationCategory.Success] = new()
            {
                BackgroundColor = System.Windows.Media.Color.FromRgb(240, 255, 240),
                AccentColor = System.Windows.Media.Color.FromRgb(76, 175, 80),
                IconContent = "✅",
                DefaultTimeout = TimeSpan.FromSeconds(4),
                AllowAutoDismiss = true
            },
            [NotificationCategory.Warning] = new()
            {
                BackgroundColor = System.Windows.Media.Color.FromRgb(255, 251, 230),
                AccentColor = System.Windows.Media.Color.FromRgb(255, 193, 7),
                IconContent = "⚠️",
                DefaultTimeout = TimeSpan.FromSeconds(7),
                AllowAutoDismiss = true
            },
            [NotificationCategory.Error] = new()
            {
                BackgroundColor = System.Windows.Media.Color.FromRgb(255, 242, 242),
                AccentColor = System.Windows.Media.Color.FromRgb(244, 67, 54),
                IconContent = "❌",
                DefaultTimeout = TimeSpan.FromSeconds(10),
                AllowAutoDismiss = false // Errors require manual dismissal by default
            },
            [NotificationCategory.System] = new()
            {
                BackgroundColor = System.Windows.Media.Color.FromRgb(248, 248, 248),
                AccentColor = System.Windows.Media.Color.FromRgb(158, 158, 158),
                IconContent = "⚙️",
                DefaultTimeout = TimeSpan.FromSeconds(6),
                AllowAutoDismiss = true
            },
            [NotificationCategory.Custom] = new()
            {
                BackgroundColor = System.Windows.Media.Color.FromRgb(245, 245, 245),
                AccentColor = System.Windows.Media.Color.FromRgb(96, 96, 96),
                IconContent = "📋",
                DefaultTimeout = TimeSpan.FromSeconds(5),
                AllowAutoDismiss = true
            }
        };
    }

    /// <summary>
    /// Configuration for priority-based behavior
    /// </summary>
    public class PriorityConfiguration
    {
        /// <summary>
        /// Timeout multiplier for this priority level
        /// </summary>
        public double TimeoutMultiplier { get; set; } = 1.0;

        /// <summary>
        /// Whether this priority requires manual dismissal
        /// </summary>
        public bool RequireManualDismiss { get; set; } = false;

        /// <summary>
        /// Visual emphasis factor (affects size, glow, etc.)
        /// </summary>
        public double VisualEmphasis { get; set; } = 1.0;

        /// <summary>
        /// Stack order priority (higher numbers appear on top)
        /// </summary>
        public int StackOrder { get; set; } = 0;

        /// <summary>
        /// Whether to show priority indicator badge
        /// </summary>
        public bool ShowPriorityIndicator { get; set; } = false;

        /// <summary>
        /// Gets default priority configurations
        /// </summary>
        public static Dictionary<NotificationPriority, PriorityConfiguration> GetDefaults() => new()
        {
            [NotificationPriority.Low] = new()
            {
                TimeoutMultiplier = 1.5,
                RequireManualDismiss = false,
                VisualEmphasis = 0.9,
                StackOrder = 1,
                ShowPriorityIndicator = false
            },
            [NotificationPriority.Normal] = new()
            {
                TimeoutMultiplier = 1.0,
                RequireManualDismiss = false,
                VisualEmphasis = 1.0,
                StackOrder = 2,
                ShowPriorityIndicator = false
            },
            [NotificationPriority.High] = new()
            {
                TimeoutMultiplier = 0.8,
                RequireManualDismiss = false,
                VisualEmphasis = 1.1,
                StackOrder = 3,
                ShowPriorityIndicator = true
            },
            [NotificationPriority.Critical] = new()
            {
                TimeoutMultiplier = 0.0, // No auto-dismiss by default
                RequireManualDismiss = true,
                VisualEmphasis = 1.2,
                StackOrder = 4,
                ShowPriorityIndicator = true
            }
        };
    }

    /// <summary>
    /// Summary information for overflow handling
    /// </summary>
    public class NotificationSummary
    {
        /// <summary>
        /// Total number of notifications represented by this summary
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Breakdown by category
        /// </summary>
        public Dictionary<NotificationCategory, int> CategoryCounts { get; set; } = new();

        /// <summary>
        /// Breakdown by priority
        /// </summary>
        public Dictionary<NotificationPriority, int> PriorityCounts { get; set; } = new();

        /// <summary>
        /// Most recent notification timestamp
        /// </summary>
        public DateTime MostRecentTime { get; set; }

        /// <summary>
        /// Oldest notification timestamp
        /// </summary>
        public DateTime OldestTime { get; set; }

        /// <summary>
        /// Generates a user-friendly summary message
        /// </summary>
        public string GenerateSummaryMessage()
        {
            if (TotalCount == 0) return "No notifications";

            var message = $"{TotalCount} more notification{(TotalCount > 1 ? "s" : "")}";

            // Add category breakdown for meaningful counts
            var significantCategories = CategoryCounts.Where(c => c.Value > 0).ToList();
            if (significantCategories.Count > 0 && significantCategories.Count <= 3)
            {
                var categoryParts = significantCategories.Select(c => $"{c.Value} {c.Key.ToString().ToLower()}");
                message += $": {string.Join(", ", categoryParts)}";
            }

            return message;
        }

        /// <summary>
        /// Determines the most appropriate summary category based on content
        /// </summary>
        public NotificationCategory GetDominantCategory()
        {
            if (CategoryCounts.Count == 0) return NotificationCategory.Info;

            // Prioritize errors and warnings, then highest count
            if (CategoryCounts.GetValueOrDefault(NotificationCategory.Error, 0) > 0)
                return NotificationCategory.Error;

            if (CategoryCounts.GetValueOrDefault(NotificationCategory.Warning, 0) > 0)
                return NotificationCategory.Warning;

            return CategoryCounts.OrderByDescending(c => c.Value).First().Key;
        }
    }
}