using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace IntervalToast
{
    /// <summary>
    /// Centralized manager for notification queue, overflow handling, and display coordination.
    /// Provides enterprise-level notification management with priority ordering, category grouping,
    /// and intelligent overflow handling.
    /// </summary>
    public sealed class NotificationManager : IDisposable
    {
        #region Singleton Pattern

        private static readonly Lazy<NotificationManager> _instance = new(() => new NotificationManager());

        /// <summary>
        /// Gets the singleton instance of the NotificationManager
        /// </summary>
        public static NotificationManager Instance => _instance.Value;

        #endregion

        #region Fields

        private readonly ConcurrentQueue<NotificationData> _pendingQueue = new();
        private readonly List<ActiveNotification> _activeNotifications = new();
        private readonly WindowPositionManager _positionManager = new();
        private readonly object _lockObject = new();
        private readonly System.Threading.Timer _queueProcessor;
        private readonly DispatcherTimer _summaryUpdateTimer;
        private NotificationConfiguration _globalConfiguration = new();
        private bool _disposed = false;
        private NotificationWindow? _summaryWindow;

        #endregion

        #region Events

        /// <summary>
        /// Raised when a notification is shown
        /// </summary>
        public event EventHandler<NotificationEventArgs>? NotificationShown;

        /// <summary>
        /// Raised when a notification is closed
        /// </summary>
        public event EventHandler<NotificationEventArgs>? NotificationClosed;

        /// <summary>
        /// Raised when the queue overflows
        /// </summary>
        public event EventHandler<OverflowEventArgs>? QueueOverflow;

        /// <summary>
        /// Raised when notifications are batched/grouped
        /// </summary>
        public event EventHandler<BatchEventArgs>? NotificationsBatched;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the NotificationManager class
        /// </summary>
        private NotificationManager()
        {
            // Initialize queue processor timer
            _queueProcessor = new System.Threading.Timer(ProcessQueue, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));

            // Initialize summary update timer
            _summaryUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _summaryUpdateTimer.Tick += UpdateSummaryNotification;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows a notification with the specified data
        /// </summary>
        /// <param name="notificationData">The notification data to display</param>
        /// <param name="configuration">Optional configuration override</param>
        public void ShowNotification(NotificationData notificationData, NotificationConfiguration? configuration = null)
        {
            if (notificationData == null)
                throw new ArgumentNullException(nameof(notificationData));

            var config = configuration ?? _globalConfiguration;

            // Apply category filtering if enabled
            if (config.EnableCategoryFiltering && config.ActiveCategoryFilter.HasValue &&
                config.ActiveCategoryFilter.Value != notificationData.Category)
            {
                return; // Skip filtered notifications
            }

            // Check if we can show immediately or need to queue
            lock (_lockObject)
            {
                if (CanShowImmediately(config))
                {
                    ShowNotificationImmediately(notificationData, config);
                }
                else
                {
                    EnqueueNotification(notificationData, config);
                }
            }
        }

        /// <summary>
        /// Shows a simple notification with title and message
        /// </summary>
        /// <param name="title">The notification title</param>
        /// <param name="message">The notification message</param>
        /// <param name="category">The notification category</param>
        /// <param name="priority">The notification priority</param>
        public void ShowNotification(string title, string message,
            NotificationCategory category = NotificationCategory.Info,
            NotificationPriority priority = NotificationPriority.Normal)
        {
            var data = new NotificationData
            {
                Title = title,
                Message = message,
                Category = category,
                Priority = priority
            };

            ShowNotification(data);
        }

        /// <summary>
        /// Closes a specific notification by ID
        /// </summary>
        /// <param name="notificationId">The ID of the notification to close</param>
        public void CloseNotification(Guid notificationId)
        {
            lock (_lockObject)
            {
                var activeNotification = _activeNotifications.FirstOrDefault(n => n.Data.Id == notificationId);
                if (activeNotification != null)
                {
                    activeNotification.Window.CloseNotification();
                }
            }
        }

        /// <summary>
        /// Closes all notifications
        /// </summary>
        public void CloseAllNotifications()
        {
            lock (_lockObject)
            {
                // Close all active notifications
                var notifications = _activeNotifications.ToList();
                foreach (var notification in notifications)
                {
                    notification.Window.CloseNotification();
                }

                // Clear the queue
                while (_pendingQueue.TryDequeue(out _)) { }

                // Close summary notification
                CloseSummaryNotification();
            }
        }

        /// <summary>
        /// Closes all notifications of a specific category
        /// </summary>
        /// <param name="category">The category to close</param>
        public void CloseNotificationsByCategory(NotificationCategory category)
        {
            lock (_lockObject)
            {
                var notifications = _activeNotifications
                    .Where(n => n.Data.Category == category)
                    .ToList();

                foreach (var notification in notifications)
                {
                    notification.Window.CloseNotification();
                }
            }
        }

        /// <summary>
        /// Closes all notifications of a specific priority
        /// </summary>
        /// <param name="priority">The priority to close</param>
        public void CloseNotificationsByPriority(NotificationPriority priority)
        {
            lock (_lockObject)
            {
                var notifications = _activeNotifications
                    .Where(n => n.Data.Priority == priority)
                    .ToList();

                foreach (var notification in notifications)
                {
                    notification.Window.CloseNotification();
                }
            }
        }

        /// <summary>
        /// Gets the current queue status
        /// </summary>
        /// <returns>Queue status information</returns>
        public QueueStatus GetQueueStatus()
        {
            lock (_lockObject)
            {
                return new QueueStatus
                {
                    ActiveCount = _activeNotifications.Count,
                    PendingCount = _pendingQueue.Count,
                    TotalCount = _activeNotifications.Count + _pendingQueue.Count,
                    MaxVisible = _globalConfiguration.MaxVisibleNotifications,
                    MaxQueue = _globalConfiguration.MaxQueueSize,
                    IsOverflowing = _pendingQueue.Count > 0
                };
            }
        }

        /// <summary>
        /// Sets the global configuration for notifications
        /// </summary>
        /// <param name="configuration">The configuration to use</param>
        public void SetGlobalConfiguration(NotificationConfiguration configuration)
        {
            _globalConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Update timer intervals
            _queueProcessor.Change(TimeSpan.Zero, _globalConfiguration.QueueProcessingDelay);

            // Restart summary timer if enabled
            if (_globalConfiguration.EnableSummaryNotifications)
            {
                _summaryUpdateTimer.Start();
            }
            else
            {
                _summaryUpdateTimer.Stop();
                CloseSummaryNotification();
            }
        }

        /// <summary>
        /// Gets the current global configuration
        /// </summary>
        /// <returns>The current global configuration</returns>
        public NotificationConfiguration GetGlobalConfiguration()
        {
            return _globalConfiguration.Clone();
        }

        /// <summary>
        /// Gets all active notifications
        /// </summary>
        /// <returns>Read-only list of active notification data</returns>
        public IReadOnlyList<NotificationData> GetActiveNotifications()
        {
            lock (_lockObject)
            {
                return _activeNotifications.Select(n => n.Data).ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Gets all pending notifications in the queue
        /// </summary>
        /// <returns>Read-only list of pending notification data</returns>
        public IReadOnlyList<NotificationData> GetPendingNotifications()
        {
            return _pendingQueue.ToList().AsReadOnly();
        }

        /// <summary>
        /// Closes the most recent notification by creation time
        /// </summary>
        /// <returns>True if a notification was closed, false if no notifications exist</returns>
        public bool CloseLatestNotification()
        {
            lock (_lockObject)
            {
                var activeNotifications = _activeNotifications.ToList();
                if (activeNotifications.Count == 0)
                    return false;

                var latest = activeNotifications.OrderByDescending(n => n.CreatedAt).First();
                latest.Window.CloseNotification();
                return true;
            }
        }

        /// <summary>
        /// Gets the most recent notification data without closing it
        /// </summary>
        /// <returns>The most recent notification data, or null if no notifications exist</returns>
        public NotificationData? GetLatestNotification()
        {
            lock (_lockObject)
            {
                var activeNotifications = _activeNotifications.ToList();
                if (activeNotifications.Count == 0)
                    return null;

                return activeNotifications.OrderByDescending(n => n.CreatedAt).First().Data;
            }
        }

        /// <summary>
        /// Gets comprehensive status information including system state
        /// </summary>
        /// <returns>Enhanced queue status with additional system information</returns>
        public EnhancedQueueStatus GetEnhancedQueueStatus()
        {
            lock (_lockObject)
            {
                var basicStatus = GetQueueStatus();
                var activeNotifications = _activeNotifications.Select(n => n.Data).ToList();

                var categoryBreakdown = activeNotifications
                    .GroupBy(n => n.Category)
                    .ToDictionary(g => g.Key, g => g.Count());

                var priorityBreakdown = activeNotifications
                    .GroupBy(n => n.Priority)
                    .ToDictionary(g => g.Key, g => g.Count());

                var oldestActive = activeNotifications.Count > 0
                    ? activeNotifications.Min(n => n.CreatedAt)
                    : (DateTime?)null;

                var newestActive = activeNotifications.Count > 0
                    ? activeNotifications.Max(n => n.CreatedAt)
                    : (DateTime?)null;

                return new EnhancedQueueStatus
                {
                    ActiveCount = basicStatus.ActiveCount,
                    PendingCount = basicStatus.PendingCount,
                    TotalCount = basicStatus.TotalCount,
                    MaxVisible = basicStatus.MaxVisible,
                    MaxQueue = basicStatus.MaxQueue,
                    IsOverflowing = basicStatus.IsOverflowing,
                    CategoryBreakdown = categoryBreakdown,
                    PriorityBreakdown = priorityBreakdown,
                    OldestActiveTime = oldestActive,
                    NewestActiveTime = newestActive,
                    SystemEnabled = true, // Default - can be enhanced later
                    HasSummaryNotification = _summaryWindow != null
                };
            }
        }

        /// <summary>
        /// Closes all notifications and provides detailed information about what was closed
        /// </summary>
        /// <returns>Information about the notifications that were closed</returns>
        public NotificationDismissalResult CloseAllNotificationsWithResult()
        {
            lock (_lockObject)
            {
                var activeNotifications = _activeNotifications.ToList();
                var pendingNotifications = _pendingQueue.ToList();
                var summaryActive = _summaryWindow != null;

                var result = new NotificationDismissalResult
                {
                    ActiveNotificationsClosed = activeNotifications.Count,
                    PendingNotificationsCleared = pendingNotifications.Count,
                    SummaryNotificationClosed = summaryActive,
                    TotalNotificationsAffected = activeNotifications.Count + pendingNotifications.Count,
                    ClosedNotifications = activeNotifications.Select(n => n.Data).ToList()
                };

                // Perform the actual closing
                CloseAllNotifications();

                return result;
            }
        }

        /// <summary>
        /// Shows a system feedback notification with enhanced configuration
        /// </summary>
        /// <param name="title">The feedback title</param>
        /// <param name="message">The feedback message</param>
        /// <param name="duration">Custom duration for the feedback</param>
        /// <param name="category">Category for the feedback notification</param>
        public void ShowSystemFeedback(string title, string message,
            TimeSpan? duration = null, NotificationCategory category = NotificationCategory.System)
        {
            var feedbackData = new NotificationData
            {
                Title = title,
                Message = message,
                Category = category,
                Priority = NotificationPriority.Low,
                CustomTimeout = duration ?? TimeSpan.FromSeconds(2)
            };

            // Add metadata to identify as system feedback
            feedbackData.Metadata["IsSystemFeedback"] = true;
            feedbackData.Metadata["FeedbackTimestamp"] = DateTime.Now;

            ShowNotification(feedbackData);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Determines if a notification can be shown immediately.
        /// Supports multiple notifications up to MaxVisibleNotifications limit.
        /// </summary>
        private bool CanShowImmediately(NotificationConfiguration config)
        {
            return _activeNotifications.Count < config.MaxVisibleNotifications;
        }

        /// <summary>
        /// Shows a notification immediately without queuing
        /// </summary>
        private void ShowNotificationImmediately(NotificationData data, NotificationConfiguration config)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    var windowConfig = CreateWindowConfiguration(data, config);
                    var window = new NotificationWindow(windowConfig);

                    // Apply category and priority styling
                    ApplyCategoryAndPriorityStyling(window, data, config);

                    var activeNotification = new ActiveNotification
                    {
                        Data = data,
                        Window = window,
                        CreatedAt = DateTime.Now
                    };

                    // Subscribe to window events
                    window.NotificationClosed += (sender, e) => OnNotificationClosed(activeNotification);

                    // Add to active list
                    lock (_lockObject)
                    {
                        _activeNotifications.Add(activeNotification);
                    }

                    // Calculate position for vertical stacking
                    var stackIndex = _activeNotifications.Count;
                    var position = _positionManager.CalculatePosition(window, config.Position, stackIndex, config);
                    window.Left = position.X;
                    window.Top = position.Y;

                    // Show the notification
                    window.ShowNotification(data.Title, data.Message);

                    // Raise event
                    NotificationShown?.Invoke(this, new NotificationEventArgs(data));

                    // Update summary if needed
                    UpdateSummaryNotificationIfNeeded();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error showing notification: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Enqueues a notification for later display
        /// </summary>
        private void EnqueueNotification(NotificationData data, NotificationConfiguration config)
        {
            // Check queue capacity
            if (_pendingQueue.Count >= config.MaxQueueSize)
            {
                HandleQueueOverflow(data, config);
                return;
            }

            _pendingQueue.Enqueue(data);
            UpdateSummaryNotificationIfNeeded();
        }

        /// <summary>
        /// Handles queue overflow based on configuration
        /// </summary>
        private void HandleQueueOverflow(NotificationData newData, NotificationConfiguration config)
        {
            switch (config.OverflowStrategy)
            {
                case OverflowHandling.Queue:
                    // Drop the new notification
                    QueueOverflow?.Invoke(this, new OverflowEventArgs(newData, _pendingQueue.Count));
                    break;

                case OverflowHandling.Replace:
                    // Remove oldest and add new
                    _pendingQueue.TryDequeue(out var _);
                    _pendingQueue.Enqueue(newData);
                    break;

                case OverflowHandling.Drop:
                    // Just drop the new notification
                    break;

                case OverflowHandling.Compress:
                    // Enable compact mode and try to fit more
                    if (!config.EnableCompactMode)
                    {
                        config.EnableCompactMode = true;
                        _pendingQueue.Enqueue(newData);
                    }
                    break;
            }
        }

        /// <summary>
        /// Processes the pending queue
        /// </summary>
        private void ProcessQueue(object? state)
        {
            if (_disposed) return;

            try
            {
                lock (_lockObject)
                {
                    while (_pendingQueue.TryPeek(out var nextData) && CanShowImmediately(_globalConfiguration))
                    {
                        if (_pendingQueue.TryDequeue(out var data))
                        {
                            ShowNotificationImmediately(data, _globalConfiguration);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing queue: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles notification closed event and repositions remaining notifications
        /// </summary>
        private void OnNotificationClosed(ActiveNotification activeNotification)
        {
            int removedIndex;
            List<ActiveNotification> remainingNotifications;

            lock (_lockObject)
            {
                removedIndex = _activeNotifications.IndexOf(activeNotification);
                _activeNotifications.Remove(activeNotification);
                remainingNotifications = new List<ActiveNotification>(_activeNotifications);
                NotificationClosed?.Invoke(this, new NotificationEventArgs(activeNotification.Data));
            }

            // Reposition remaining notifications if repositioning is enabled
            if (_globalConfiguration.EnableRepositionAnimations && remainingNotifications.Count > 0 && removedIndex >= 0)
            {
                RepositionNotifications(remainingNotifications, removedIndex);
            }

            // Update summary
            UpdateSummaryNotificationIfNeeded();
        }

        /// <summary>
        /// Repositions remaining notifications after one has been dismissed
        /// </summary>
        /// <param name=\"remainingNotifications\">List of notifications to reposition</param>
        /// <param name=\"removedIndex\">Index of the removed notification</param>
        private void RepositionNotifications(List<ActiveNotification> remainingNotifications, int removedIndex)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    // Get current positions
                    var currentPositions = remainingNotifications.Select(n => new System.Windows.Point(n.Window.Left, n.Window.Top)).ToArray();

                    // Calculate new positions
                    var newPositions = _positionManager.CalculateRepositionedPositions(
                        currentPositions,
                        removedIndex,
                        _globalConfiguration.Position,
                        _globalConfiguration);

                    // Animate notifications to new positions
                    for (int i = 0; i < remainingNotifications.Count && i < newPositions.Length; i++)
                    {
                        var notification = remainingNotifications[i];
                        var newPosition = newPositions[i];

                        // Only animate if position actually changed
                        if (Math.Abs(notification.Window.Left - newPosition.X) > 1 ||
                            Math.Abs(notification.Window.Top - newPosition.Y) > 1)
                        {
                            AnimateNotificationPosition(notification.Window, newPosition);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error repositioning notifications: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Animates a notification window to a new position
        /// </summary>
        /// <param name=\"window\">The notification window to animate</param>
        /// <param name=\"newPosition\">The target position</param>
        private void AnimateNotificationPosition(NotificationWindow window, System.Windows.Point newPosition)
        {
            try
            {
                var duration = _globalConfiguration.RepositionAnimationDuration;

                // Create smooth position animation
                var leftAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = window.Left,
                    To = newPosition.X,
                    Duration = duration,
                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };

                var topAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = window.Top,
                    To = newPosition.Y,
                    Duration = duration,
                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };

                // Apply animations
                window.BeginAnimation(System.Windows.Window.LeftProperty, leftAnimation);
                window.BeginAnimation(System.Windows.Window.TopProperty, topAnimation);
            }
            catch (Exception ex)
            {
                // Fallback to immediate positioning if animation fails
                System.Diagnostics.Debug.WriteLine($"Animation failed, using immediate positioning: {ex.Message}");
                window.Left = newPosition.X;
                window.Top = newPosition.Y;
            }
        }


        /// <summary>
        /// Creates window configuration with simplified uniform sizing.
        /// Category colors are preserved but sizing is uniform.
        /// </summary>
        private NotificationConfiguration CreateWindowConfiguration(NotificationData data, NotificationConfiguration baseConfig)
        {
            var config = baseConfig.Clone();

            // Force uniform sizing regardless of other configurations
            config.Width = 350.0;
            config.Height = 120.0;

            // Apply category configuration for visual theming only
            if (config.CategoryConfigurations.TryGetValue(data.Category, out var categoryConfig))
            {
                config.BackgroundColor = categoryConfig.BackgroundColor;
                config.AccentColor = categoryConfig.AccentColor;

                if (data.CustomTimeout.HasValue)
                {
                    config.AutoCloseDelay = data.CustomTimeout.Value;
                }
                else if (categoryConfig.AllowAutoDismiss)
                {
                    config.AutoCloseDelay = categoryConfig.DefaultTimeout;
                }
                else
                {
                    config.AutoCloseDelay = TimeSpan.Zero; // Manual dismiss only
                }
            }

            // Apply priority timeout configuration only (no visual scaling)
            if (config.PriorityConfigurations.TryGetValue(data.Priority, out var priorityConfig))
            {
                if (priorityConfig.RequireManualDismiss)
                {
                    config.AutoCloseDelay = TimeSpan.Zero;
                }
                else if (config.AutoCloseDelay > TimeSpan.Zero)
                {
                    var timeoutMultiplier = priorityConfig.TimeoutMultiplier;
                    config.AutoCloseDelay = TimeSpan.FromMilliseconds(config.AutoCloseDelay.TotalMilliseconds * timeoutMultiplier);
                }
                // Visual emphasis removed for uniform sizing
            }

            // Disable all sizing modifications
            config.EnableCompactMode = false;
            config.EnableDynamicSizing = false;

            return config;
        }

        /// <summary>
        /// Applies category and priority styling to a notification window
        /// </summary>
        private void ApplyCategoryAndPriorityStyling(NotificationWindow window, NotificationData data, NotificationConfiguration config)
        {
            // Set category-specific icon if available
            if (config.CategoryConfigurations.TryGetValue(data.Category, out var categoryConfig))
            {
                if (!string.IsNullOrEmpty(data.IconContent))
                {
                    // Use custom icon from data
                }
                else if (!string.IsNullOrEmpty(categoryConfig.IconContent))
                {
                    // Use category default icon
                }
            }
        }

        /// <summary>
        /// Updates the summary notification if needed
        /// </summary>
        private void UpdateSummaryNotificationIfNeeded()
        {
            if (!_globalConfiguration.EnableSummaryNotifications) return;

            var pendingCount = _pendingQueue.Count;
            var hasOverflow = pendingCount > 0;

            if (hasOverflow && _summaryWindow == null)
            {
                CreateSummaryNotification();
            }
            else if (!hasOverflow && _summaryWindow != null)
            {
                CloseSummaryNotification();
            }
            else if (hasOverflow && _summaryWindow != null)
            {
                UpdateSummaryContent();
            }
        }

        /// <summary>
        /// Creates a summary notification for overflow
        /// </summary>
        private void CreateSummaryNotification()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    var summary = GenerateNotificationSummary();
                    var summaryData = new NotificationData
                    {
                        Title = "More Notifications",
                        Message = summary.GenerateSummaryMessage(),
                        Category = summary.GetDominantCategory(),
                        Priority = NotificationPriority.Low
                    };

                    var config = CreateWindowConfiguration(summaryData, _globalConfiguration);
                    config.AutoCloseDelay = TimeSpan.Zero; // Manual dismiss only
                    config.ShowCloseButton = true;

                    _summaryWindow = new NotificationWindow(config);
                    _summaryWindow.NotificationClosed += (s, e) => _summaryWindow = null;
                    _summaryWindow.ShowNotification(summaryData.Title, summaryData.Message);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating summary notification: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Updates the content of the summary notification
        /// </summary>
        private void UpdateSummaryContent()
        {
            if (_summaryWindow == null) return;

            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    var summary = GenerateNotificationSummary();
                    _summaryWindow.NotificationMessage = summary.GenerateSummaryMessage();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating summary notification: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Closes the summary notification
        /// </summary>
        private void CloseSummaryNotification()
        {
            if (_summaryWindow != null)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    _summaryWindow?.CloseNotification();
                    _summaryWindow = null;
                });
            }
        }

        /// <summary>
        /// Generates a summary of pending notifications
        /// </summary>
        private NotificationSummary GenerateNotificationSummary()
        {
            var pendingList = _pendingQueue.ToList();
            var summary = new NotificationSummary
            {
                TotalCount = pendingList.Count
            };

            if (pendingList.Count > 0)
            {
                summary.MostRecentTime = pendingList.Max(n => n.CreatedAt);
                summary.OldestTime = pendingList.Min(n => n.CreatedAt);

                foreach (var notification in pendingList)
                {
                    summary.CategoryCounts[notification.Category] =
                        summary.CategoryCounts.GetValueOrDefault(notification.Category, 0) + 1;

                    summary.PriorityCounts[notification.Priority] =
                        summary.PriorityCounts.GetValueOrDefault(notification.Priority, 0) + 1;
                }
            }

            return summary;
        }

        /// <summary>
        /// Updates summary notification timer callback
        /// </summary>
        private void UpdateSummaryNotification(object? sender, EventArgs e)
        {
            UpdateSummaryNotificationIfNeeded();
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of the NotificationManager resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _queueProcessor?.Dispose();
                _summaryUpdateTimer?.Stop();
                CloseAllNotifications();
                _disposed = true;
            }
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Represents an active notification with its window and metadata
    /// </summary>
    internal class ActiveNotification
    {
        public NotificationData Data { get; set; } = null!;
        public NotificationWindow Window { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Event arguments for notification events
    /// </summary>
    public class NotificationEventArgs : EventArgs
    {
        public NotificationData Data { get; }

        public NotificationEventArgs(NotificationData data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// Event arguments for overflow events
    /// </summary>
    public class OverflowEventArgs : EventArgs
    {
        public NotificationData DroppedNotification { get; }
        public int QueueSize { get; }

        public OverflowEventArgs(NotificationData droppedNotification, int queueSize)
        {
            DroppedNotification = droppedNotification;
            QueueSize = queueSize;
        }
    }

    /// <summary>
    /// Event arguments for batch events
    /// </summary>
    public class BatchEventArgs : EventArgs
    {
        public IReadOnlyList<NotificationData> BatchedNotifications { get; }

        public BatchEventArgs(IReadOnlyList<NotificationData> batchedNotifications)
        {
            BatchedNotifications = batchedNotifications;
        }
    }

    /// <summary>
    /// Queue status information
    /// </summary>
    public class QueueStatus
    {
        public int ActiveCount { get; set; }
        public int PendingCount { get; set; }
        public int TotalCount { get; set; }
        public int MaxVisible { get; set; }
        public int MaxQueue { get; set; }
        public bool IsOverflowing { get; set; }
    }

    /// <summary>
    /// Enhanced queue status with additional system information for hotkey actions
    /// </summary>
    public class EnhancedQueueStatus : QueueStatus
    {
        /// <summary>
        /// Breakdown of active notifications by category
        /// </summary>
        public Dictionary<NotificationCategory, int> CategoryBreakdown { get; set; } = new();

        /// <summary>
        /// Breakdown of active notifications by priority
        /// </summary>
        public Dictionary<NotificationPriority, int> PriorityBreakdown { get; set; } = new();

        /// <summary>
        /// Timestamp of the oldest active notification
        /// </summary>
        public DateTime? OldestActiveTime { get; set; }

        /// <summary>
        /// Timestamp of the newest active notification
        /// </summary>
        public DateTime? NewestActiveTime { get; set; }

        /// <summary>
        /// Whether the notification system is currently enabled
        /// </summary>
        public bool SystemEnabled { get; set; }

        /// <summary>
        /// Whether a summary notification is currently displayed
        /// </summary>
        public bool HasSummaryNotification { get; set; }

        /// <summary>
        /// Age of the oldest active notification
        /// </summary>
        public TimeSpan? OldestNotificationAge => OldestActiveTime.HasValue
            ? DateTime.Now - OldestActiveTime.Value
            : null;

        /// <summary>
        /// Generates a comprehensive status message
        /// </summary>
        public string GenerateDetailedStatusMessage()
        {
            var message = $"Active: {ActiveCount}/{MaxVisible}\n" +
                         $"Pending: {PendingCount}/{MaxQueue}\n" +
                         $"Total: {TotalCount}";

            if (IsOverflowing)
            {
                message += "\nQueue is overflowing";
            }

            message += $"\nSystem: {(SystemEnabled ? "Enabled" : "Disabled")}";

            if (CategoryBreakdown.Count > 0)
            {
                var categoryParts = CategoryBreakdown
                    .Where(c => c.Value > 0)
                    .Select(c => $"{c.Key}: {c.Value}")
                    .ToList();

                if (categoryParts.Count > 0)
                {
                    message += $"\nCategories: {string.Join(", ", categoryParts)}";
                }
            }

            if (OldestNotificationAge.HasValue)
            {
                var age = OldestNotificationAge.Value;
                var ageText = age.TotalMinutes >= 1
                    ? $"{age.TotalMinutes:F0}m"
                    : $"{age.TotalSeconds:F0}s";
                message += $"\nOldest: {ageText} ago";
            }

            return message;
        }
    }

    /// <summary>
    /// Result information from closing notifications
    /// </summary>
    public class NotificationDismissalResult
    {
        /// <summary>
        /// Number of active notifications that were closed
        /// </summary>
        public int ActiveNotificationsClosed { get; set; }

        /// <summary>
        /// Number of pending notifications that were cleared from queue
        /// </summary>
        public int PendingNotificationsCleared { get; set; }

        /// <summary>
        /// Whether a summary notification was closed
        /// </summary>
        public bool SummaryNotificationClosed { get; set; }

        /// <summary>
        /// Total number of notifications affected
        /// </summary>
        public int TotalNotificationsAffected { get; set; }

        /// <summary>
        /// List of notification data that was closed (active notifications only)
        /// </summary>
        public List<NotificationData> ClosedNotifications { get; set; } = new();

        /// <summary>
        /// Whether any notifications were actually dismissed
        /// </summary>
        public bool AnyNotificationsDismissed => TotalNotificationsAffected > 0;

        /// <summary>
        /// Generates a user-friendly summary of what was dismissed
        /// </summary>
        public string GenerateSummaryMessage()
        {
            if (!AnyNotificationsDismissed)
            {
                return "No notifications to dismiss";
            }

            if (TotalNotificationsAffected == 1)
            {
                return "Dismissed 1 notification";
            }

            var message = $"Dismissed {TotalNotificationsAffected} notifications";

            if (ActiveNotificationsClosed > 0 && PendingNotificationsCleared > 0)
            {
                message += $" ({ActiveNotificationsClosed} active, {PendingNotificationsCleared} pending)";
            }

            return message;
        }
    }

    #endregion
}