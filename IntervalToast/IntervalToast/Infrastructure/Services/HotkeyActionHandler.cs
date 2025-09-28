using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using IntervalToast.Domain.Entities;
using IntervalToast.Domain.Enums;

namespace IntervalToast.Infrastructure.Services
{
    /// <summary>
    /// Enumeration of available hotkey actions for notification system control
    /// </summary>
    public enum HotkeyAction
    {
        /// <summary>Close all active notifications with feedback</summary>
        DismissAll,
        /// <summary>Close the most recent notification</summary>
        DismissLatest,
        /// <summary>Enable/disable notification system</summary>
        ToggleSystem,
        /// <summary>Display current queue status as notification</summary>
        ShowQueueStatus,
        /// <summary>Custom action defined by user</summary>
        Custom
    }

    /// <summary>
    /// Configuration for hotkey action behavior and feedback
    /// </summary>
    public class HotkeyActionConfiguration
    {
        /// <summary>
        /// Whether to show visual feedback notifications for this action
        /// </summary>
        public bool ShowFeedback { get; set; } = true;

        /// <summary>
        /// Whether to show feedback even when no action was needed (e.g., dismiss all when no notifications exist)
        /// </summary>
        public bool ShowFeedbackOnNoOp { get; set; } = false;

        /// <summary>
        /// Custom feedback title override
        /// </summary>
        public string? CustomFeedbackTitle { get; set; }

        /// <summary>
        /// Duration to show feedback notifications (null uses default)
        /// </summary>
        public TimeSpan? FeedbackDuration { get; set; }

        /// <summary>
        /// Category to use for feedback notifications
        /// </summary>
        public NotificationCategory FeedbackCategory { get; set; } = NotificationCategory.System;

        /// <summary>
        /// Whether this action is currently enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Creates default configuration for a specific action
        /// </summary>
        public static HotkeyActionConfiguration CreateDefault(HotkeyAction action)
        {
            return action switch
            {
                HotkeyAction.DismissAll => new()
                {
                    ShowFeedback = true,
                    ShowFeedbackOnNoOp = false,
                    CustomFeedbackTitle = "Notifications Dismissed",
                    FeedbackCategory = NotificationCategory.System,
                    FeedbackDuration = TimeSpan.FromSeconds(2)
                },
                HotkeyAction.DismissLatest => new()
                {
                    ShowFeedback = true,
                    ShowFeedbackOnNoOp = false,
                    CustomFeedbackTitle = "Latest Notification Dismissed",
                    FeedbackCategory = NotificationCategory.System,
                    FeedbackDuration = TimeSpan.FromSeconds(1.5)
                },
                HotkeyAction.ToggleSystem => new()
                {
                    ShowFeedback = true,
                    ShowFeedbackOnNoOp = true,
                    CustomFeedbackTitle = "Notification System",
                    FeedbackCategory = NotificationCategory.Info,
                    FeedbackDuration = TimeSpan.FromSeconds(3)
                },
                HotkeyAction.ShowQueueStatus => new()
                {
                    ShowFeedback = false, // The status display IS the feedback
                    ShowFeedbackOnNoOp = true,
                    FeedbackCategory = NotificationCategory.Info,
                    FeedbackDuration = TimeSpan.FromSeconds(5)
                },
                _ => new()
            };
        }
    }

    /// <summary>
    /// Result of executing a hotkey action
    /// </summary>
    public class HotkeyActionResult
    {
        /// <summary>
        /// Whether the action was executed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Whether any actual work was performed (e.g., false if no notifications to dismiss)
        /// </summary>
        public bool ActionPerformed { get; set; }

        /// <summary>
        /// Number of items affected (notifications dismissed, etc.)
        /// </summary>
        public int ItemsAffected { get; set; }

        /// <summary>
        /// Human-readable message describing the result
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Any error that occurred during execution
        /// </summary>
        public Exception? Error { get; set; }

        /// <summary>
        /// Additional data from the action execution
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static HotkeyActionResult CreateSuccess(string message, int itemsAffected = 0) => new()
        {
            Success = true,
            ActionPerformed = itemsAffected > 0,
            ItemsAffected = itemsAffected,
            Message = message
        };

        /// <summary>
        /// Creates a no-operation result (successful but nothing to do)
        /// </summary>
        public static HotkeyActionResult NoOp(string message) => new()
        {
            Success = true,
            ActionPerformed = false,
            ItemsAffected = 0,
            Message = message
        };

        /// <summary>
        /// Creates a failure result
        /// </summary>
        public static HotkeyActionResult Failure(string message, Exception? error = null) => new()
        {
            Success = false,
            ActionPerformed = false,
            ItemsAffected = 0,
            Message = message,
            Error = error
        };
    }

    /// <summary>
    /// Central handler for executing hotkey actions with comprehensive feedback and state management.
    /// Provides enterprise-level action execution with detailed logging, error handling, and user feedback.
    /// </summary>
    public sealed class HotkeyActionHandler : IDisposable
    {
        #region Singleton Pattern

        private static readonly Lazy<HotkeyActionHandler> _instance = new(() => new HotkeyActionHandler());

        /// <summary>
        /// Gets the singleton instance of the HotkeyActionHandler
        /// </summary>
        public static HotkeyActionHandler Instance => _instance.Value;

        #endregion

        #region Fields

        private readonly Dictionary<HotkeyAction, HotkeyActionConfiguration> _actionConfigurations = new();
        private readonly NotificationManager _notificationManager;
        private readonly object _lockObject = new();
        private bool _systemEnabled = true;
        private bool _disposed = false;

        #endregion

        #region Events

        /// <summary>
        /// Raised before an action is executed
        /// </summary>
        public event EventHandler<HotkeyActionEventArgs>? ActionExecuting;

        /// <summary>
        /// Raised after an action is executed
        /// </summary>
        public event EventHandler<HotkeyActionEventArgs>? ActionExecuted;

        /// <summary>
        /// Raised when system state changes
        /// </summary>
        public event EventHandler<SystemStateChangedEventArgs>? SystemStateChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the HotkeyActionHandler class
        /// </summary>
        private HotkeyActionHandler()
        {
            _notificationManager = NotificationManager.Instance;
            InitializeDefaultConfigurations();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets whether the notification system is currently enabled
        /// </summary>
        public bool IsSystemEnabled
        {
            get => _systemEnabled;
            private set
            {
                if (_systemEnabled != value)
                {
                    _systemEnabled = value;
                    SystemStateChanged?.Invoke(this, new SystemStateChangedEventArgs(value));
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes a hotkey action with comprehensive error handling and feedback
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <returns>Result of the action execution</returns>
        public async Task<HotkeyActionResult> ExecuteActionAsync(HotkeyAction action)
        {
            if (_disposed)
            {
                return HotkeyActionResult.Failure("Action handler has been disposed and cannot execute actions");
            }

            // Validate action parameter
            if (!Enum.IsDefined(typeof(HotkeyAction), action))
            {
                return HotkeyActionResult.Failure($"Invalid action value: {action}");
            }

            HotkeyActionConfiguration config;
            lock (_lockObject)
            {
                if (!_actionConfigurations.TryGetValue(action, out config))
                {
                    config = HotkeyActionConfiguration.CreateDefault(action);
                    _actionConfigurations[action] = config;
                }

                if (!config.IsEnabled)
                {
                    return HotkeyActionResult.Failure($"Action {action} is currently disabled");
                }
            }

            // Check system state for certain actions
            if (!ValidateSystemStateForAction(action, out var stateErrorMessage))
            {
                return HotkeyActionResult.Failure(stateErrorMessage);
            }

            var eventArgs = new HotkeyActionEventArgs(action);

            try
            {
                ActionExecuting?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                return HotkeyActionResult.Failure($"Error in ActionExecuting event handler: {ex.Message}", ex);
            }

            if (eventArgs.Cancel)
            {
                return HotkeyActionResult.Failure("Action execution was cancelled by event handler");
            }

            HotkeyActionResult result;

            try
            {
                // Add timeout protection for long-running operations
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));

                result = action switch
                {
                    HotkeyAction.DismissAll => await ExecuteDismissAllAsync(cts.Token),
                    HotkeyAction.DismissLatest => await ExecuteDismissLatestAsync(cts.Token),
                    HotkeyAction.ToggleSystem => await ExecuteToggleSystemAsync(cts.Token),
                    HotkeyAction.ShowQueueStatus => await ExecuteShowQueueStatusAsync(cts.Token),
                    HotkeyAction.Custom => HotkeyActionResult.Failure("Custom actions must be handled externally"),
                    _ => HotkeyActionResult.Failure($"Unknown action: {action}")
                };
            }
            catch (System.OperationCanceledException)
            {
                result = HotkeyActionResult.Failure($"Action {action} timed out after 30 seconds");
            }
            catch (ObjectDisposedException ex)
            {
                result = HotkeyActionResult.Failure($"Resource was disposed during action execution: {ex.Message}", ex);
            }
            catch (InvalidOperationException ex)
            {
                result = HotkeyActionResult.Failure($"Invalid operation during action execution: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                result = HotkeyActionResult.Failure($"Unexpected error executing action {action}: {ex.Message}", ex);

                // Log detailed error information for debugging
                LogDetailedError(action, ex);
            }

            // Validate result
            if (result == null)
            {
                result = HotkeyActionResult.Failure($"Action {action} returned null result");
            }

            // Show feedback if configured and appropriate
            try
            {
                if (result.Success && config.ShowFeedback &&
                    (result.ActionPerformed || config.ShowFeedbackOnNoOp))
                {
                    await ShowActionFeedbackAsync(action, result, config);
                }
            }
            catch (Exception ex)
            {
                // Don't fail the entire action if feedback fails
                LogDetailedError(action, new Exception($"Failed to show feedback: {ex.Message}", ex));
            }

            // Raise completion event
            try
            {
                var completedEventArgs = new HotkeyActionEventArgs(action) { Result = result };
                ActionExecuted?.Invoke(this, completedEventArgs);
            }
            catch (Exception ex)
            {
                // Log but don't fail the action for event handler errors
                LogDetailedError(action, new Exception($"Error in ActionExecuted event handler: {ex.Message}", ex));
            }

            return result;
        }

        /// <summary>
        /// Validates that the system is in the correct state for the specified action
        /// </summary>
        private bool ValidateSystemStateForAction(HotkeyAction action, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                switch (action)
                {
                    case HotkeyAction.DismissAll:
                    case HotkeyAction.DismissLatest:
                        // These actions are always valid, even if no notifications exist
                        return true;

                    case HotkeyAction.ToggleSystem:
                        // System toggle is always valid
                        return true;

                    case HotkeyAction.ShowQueueStatus:
                        // Status display is always valid
                        return true;

                    case HotkeyAction.Custom:
                        errorMessage = "Custom actions require external validation";
                        return false;

                    default:
                        errorMessage = $"Unknown action type: {action}";
                        return false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error validating system state: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Logs detailed error information for debugging
        /// </summary>
        private void LogDetailedError(HotkeyAction action, Exception ex)
        {
            try
            {
                var globalConfig = _notificationManager?.GetGlobalConfiguration();
                if (globalConfig?.EnableHotkeyActionLogging == true)
                {
                    var errorDetails = $"Action: {action}, " +
                                     $"Error: {ex.GetType().Name}, " +
                                     $"Message: {ex.Message}, " +
                                     $"Stack: {ex.StackTrace}";

                    System.Diagnostics.Debug.WriteLine($"[HotkeyActionHandler] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ERROR: {errorDetails}");

                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[HotkeyActionHandler] Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    }
                }
            }
            catch
            {
                // Ignore logging errors to prevent cascading failures
            }
        }

        /// <summary>
        /// Executes a hotkey action synchronously
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <returns>Result of the action execution</returns>
        public HotkeyActionResult ExecuteAction(HotkeyAction action)
        {
            return ExecuteActionAsync(action).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sets configuration for a specific action
        /// </summary>
        /// <param name="action">The action to configure</param>
        /// <param name="configuration">The configuration settings</param>
        public void SetActionConfiguration(HotkeyAction action, HotkeyActionConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            lock (_lockObject)
            {
                _actionConfigurations[action] = configuration;
            }
        }

        /// <summary>
        /// Gets configuration for a specific action
        /// </summary>
        /// <param name="action">The action to get configuration for</param>
        /// <returns>The action configuration</returns>
        public HotkeyActionConfiguration GetActionConfiguration(HotkeyAction action)
        {
            lock (_lockObject)
            {
                return _actionConfigurations.TryGetValue(action, out var config)
                    ? config
                    : HotkeyActionConfiguration.CreateDefault(action);
            }
        }

        /// <summary>
        /// Enables or disables a specific action
        /// </summary>
        /// <param name="action">The action to enable/disable</param>
        /// <param name="enabled">Whether the action should be enabled</param>
        public void SetActionEnabled(HotkeyAction action, bool enabled)
        {
            lock (_lockObject)
            {
                if (!_actionConfigurations.TryGetValue(action, out var config))
                {
                    config = HotkeyActionConfiguration.CreateDefault(action);
                    _actionConfigurations[action] = config;
                }
                config.IsEnabled = enabled;
            }
        }

        /// <summary>
        /// Gets all configured actions and their states
        /// </summary>
        /// <returns>Dictionary of actions and their configurations</returns>
        public Dictionary<HotkeyAction, HotkeyActionConfiguration> GetAllActionConfigurations()
        {
            lock (_lockObject)
            {
                return new Dictionary<HotkeyAction, HotkeyActionConfiguration>(_actionConfigurations);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes default configurations for all actions
        /// </summary>
        private void InitializeDefaultConfigurations()
        {
            foreach (HotkeyAction action in Enum.GetValues<HotkeyAction>())
            {
                if (action != HotkeyAction.Custom)
                {
                    _actionConfigurations[action] = HotkeyActionConfiguration.CreateDefault(action);
                }
            }
        }

        /// <summary>
        /// Executes the dismiss all notifications action
        /// </summary>
        private async Task<HotkeyActionResult> ExecuteDismissAllAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            try
            {
                if (_notificationManager == null)
                {
                    return HotkeyActionResult.Failure("Notification manager is not available");
                }

                cancellationToken.ThrowIfCancellationRequested();

                var dismissalResult = _notificationManager.CloseAllNotificationsWithResult();

                if (!dismissalResult.AnyNotificationsDismissed)
                {
                    return HotkeyActionResult.NoOp("No notifications to dismiss");
                }

                return HotkeyActionResult.CreateSuccess(dismissalResult.GenerateSummaryMessage(), dismissalResult.TotalNotificationsAffected);
            }
            catch (OperationCanceledException)
            {
                return HotkeyActionResult.Failure("Dismiss all operation was cancelled");
            }
            catch (Exception ex)
            {
                return HotkeyActionResult.Failure($"Failed to dismiss all notifications: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Executes the dismiss latest notification action
        /// </summary>
        private async Task<HotkeyActionResult> ExecuteDismissLatestAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            try
            {
                if (_notificationManager == null)
                {
                    return HotkeyActionResult.Failure("Notification manager is not available");
                }

                cancellationToken.ThrowIfCancellationRequested();

                var latestNotification = _notificationManager.GetLatestNotification();
                if (latestNotification == null)
                {
                    return HotkeyActionResult.NoOp("No notifications to dismiss");
                }

                var success = _notificationManager.CloseLatestNotification();
                if (!success)
                {
                    return HotkeyActionResult.Failure("Failed to dismiss latest notification");
                }

                return HotkeyActionResult.CreateSuccess("Dismissed latest notification", 1);
            }
            catch (OperationCanceledException)
            {
                return HotkeyActionResult.Failure("Dismiss latest operation was cancelled");
            }
            catch (Exception ex)
            {
                return HotkeyActionResult.Failure($"Failed to dismiss latest notification: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Executes the toggle system action
        /// </summary>
        private async Task<HotkeyActionResult> ExecuteToggleSystemAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            try
            {
                if (_notificationManager == null)
                {
                    return HotkeyActionResult.Failure("Notification manager is not available");
                }

                cancellationToken.ThrowIfCancellationRequested();

                var previousState = IsSystemEnabled;
                IsSystemEnabled = !IsSystemEnabled;

                var status = IsSystemEnabled ? "enabled" : "disabled";
                var message = $"Notification system {status}";

                int affectedItems = 0;

                // If disabling, close all notifications
                if (!IsSystemEnabled)
                {
                    var dismissalResult = _notificationManager.CloseAllNotificationsWithResult();
                    affectedItems = dismissalResult.TotalNotificationsAffected;

                    if (affectedItems > 0)
                    {
                        message += $" ({affectedItems} notification{(affectedItems > 1 ? "s" : "")} dismissed)";
                    }
                }

                var result = HotkeyActionResult.CreateSuccess(message, affectedItems);
                result.Metadata["PreviousState"] = previousState;
                result.Metadata["NewState"] = IsSystemEnabled;

                return result;
            }
            catch (OperationCanceledException)
            {
                return HotkeyActionResult.Failure("Toggle system operation was cancelled");
            }
            catch (Exception ex)
            {
                return HotkeyActionResult.Failure($"Failed to toggle notification system: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Executes the show queue status action
        /// </summary>
        private async Task<HotkeyActionResult> ExecuteShowQueueStatusAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            try
            {
                if (_notificationManager == null)
                {
                    return HotkeyActionResult.Failure("Notification manager is not available");
                }

                cancellationToken.ThrowIfCancellationRequested();

                var globalConfig = _notificationManager.GetGlobalConfiguration();
                var enhancedStatus = _notificationManager.GetEnhancedQueueStatus();

                string message;

                try
                {
                    if (globalConfig.ShowDetailedQueueStatus)
                    {
                        message = enhancedStatus.GenerateDetailedStatusMessage();
                    }
                    else
                    {
                        // Simple status message
                        message = $"Active: {enhancedStatus.ActiveCount}/{enhancedStatus.MaxVisible}\n" +
                                 $"Pending: {enhancedStatus.PendingCount}/{enhancedStatus.MaxQueue}\n" +
                                 $"Total: {enhancedStatus.TotalCount}";

                        if (enhancedStatus.IsOverflowing)
                        {
                            message += "\nQueue is overflowing";
                        }

                        message += $"\nSystem: {(IsSystemEnabled ? "Enabled" : "Disabled")}";
                    }
                }
                catch (Exception ex)
                {
                    // Fallback to basic status if detailed generation fails
                    message = $"Active: {enhancedStatus.ActiveCount}, Pending: {enhancedStatus.PendingCount}, System: {(IsSystemEnabled ? "Enabled" : "Disabled")}\n" +
                             $"(Error generating detailed status: {ex.Message})";
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Show the status as a notification
                var statusData = new NotificationData
                {
                    Title = "Queue Status",
                    Message = message,
                    Category = NotificationCategory.System,
                    Priority = NotificationPriority.Low,
                    CustomTimeout = globalConfig.QueueStatusDisplayDuration
                };

                // Add metadata for the status display
                statusData.Metadata["StatusType"] = "QueueStatus";
                statusData.Metadata["ActiveCount"] = enhancedStatus.ActiveCount;
                statusData.Metadata["PendingCount"] = enhancedStatus.PendingCount;
                statusData.Metadata["SystemEnabled"] = IsSystemEnabled;
                statusData.Metadata["IsOverflowing"] = enhancedStatus.IsOverflowing;

                _notificationManager.ShowNotification(statusData);

                var result = HotkeyActionResult.CreateSuccess("Queue status displayed", 1);
                result.Metadata["StatusData"] = enhancedStatus;

                return result;
            }
            catch (OperationCanceledException)
            {
                return HotkeyActionResult.Failure("Show queue status operation was cancelled");
            }
            catch (Exception ex)
            {
                return HotkeyActionResult.Failure($"Failed to show queue status: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Shows feedback notification for an executed action
        /// </summary>
        private async Task ShowActionFeedbackAsync(HotkeyAction action, HotkeyActionResult result,
            HotkeyActionConfiguration config)
        {
            var globalConfig = _notificationManager.GetGlobalConfiguration();

            // Check if feedback is enabled globally
            if (!globalConfig.EnableHotkeyFeedback)
                return;

            // Check for specific action type feedback settings
            if (!ShouldShowFeedbackForAction(action, result, globalConfig))
                return;

            var title = config.CustomFeedbackTitle ?? GetDefaultFeedbackTitle(action);
            var message = result.Message;

            // Enhance message with item count if configured
            if (globalConfig.IncludeItemCountInFeedback && result.ItemsAffected > 0)
            {
                if (!message.Contains(result.ItemsAffected.ToString()))
                {
                    message += $" ({result.ItemsAffected} item{(result.ItemsAffected > 1 ? "s" : "")})";
                }
            }

            var feedbackData = new NotificationData
            {
                Title = title,
                Message = message,
                Category = globalConfig.HotkeyFeedbackCategory,
                Priority = globalConfig.HotkeyFeedbackPriority,
                CustomTimeout = config.FeedbackDuration ?? globalConfig.HotkeyFeedbackDuration
            };

            // Add metadata for action context
            feedbackData.Metadata["Action"] = action.ToString();
            feedbackData.Metadata["ItemsAffected"] = result.ItemsAffected;
            feedbackData.Metadata["ActionPerformed"] = result.ActionPerformed;
            feedbackData.Metadata["IsSystemFeedback"] = true;
            feedbackData.Metadata["HotkeyActionFeedback"] = true;

            _notificationManager.ShowNotification(feedbackData);
        }

        /// <summary>
        /// Determines if feedback should be shown for a specific action based on configuration
        /// </summary>
        private bool ShouldShowFeedbackForAction(HotkeyAction action, HotkeyActionResult result, NotificationConfiguration globalConfig)
        {
            // Check no-op feedback setting
            if (!result.ActionPerformed && !globalConfig.ShowHotkeyFeedbackOnNoOp)
                return false;

            return action switch
            {
                HotkeyAction.DismissAll => globalConfig.ShowDismissFeedback,
                HotkeyAction.DismissLatest => globalConfig.ShowDismissFeedback,
                HotkeyAction.ToggleSystem => globalConfig.ShowSystemToggleFeedback,
                HotkeyAction.ShowQueueStatus => false, // Queue status is its own feedback
                _ => true
            };
        }

        /// <summary>
        /// Gets the default feedback title for an action
        /// </summary>
        private string GetDefaultFeedbackTitle(HotkeyAction action)
        {
            return action switch
            {
                HotkeyAction.DismissAll => "Notifications Dismissed",
                HotkeyAction.DismissLatest => "Latest Notification Dismissed",
                HotkeyAction.ToggleSystem => "Notification System",
                HotkeyAction.ShowQueueStatus => "Queue Status",
                _ => $"Action: {action}"
            };
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of the HotkeyActionHandler resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Event arguments for hotkey action events
    /// </summary>
    public class HotkeyActionEventArgs : EventArgs
    {
        /// <summary>
        /// The action being executed
        /// </summary>
        public HotkeyAction Action { get; }

        /// <summary>
        /// Whether to cancel the action execution (for ActionExecuting event)
        /// </summary>
        public bool Cancel { get; set; } = false;

        /// <summary>
        /// Result of the action execution (for ActionExecuted event)
        /// </summary>
        public HotkeyActionResult? Result { get; set; }

        /// <summary>
        /// Timestamp of the event
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.Now;

        /// <summary>
        /// Initializes new hotkey action event arguments
        /// </summary>
        public HotkeyActionEventArgs(HotkeyAction action)
        {
            Action = action;
        }
    }

    /// <summary>
    /// Event arguments for system state changes
    /// </summary>
    public class SystemStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Whether the system is now enabled
        /// </summary>
        public bool IsEnabled { get; }

        /// <summary>
        /// Timestamp of the state change
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.Now;

        /// <summary>
        /// Initializes new system state changed event arguments
        /// </summary>
        public SystemStateChangedEventArgs(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }
    }

    #endregion
}