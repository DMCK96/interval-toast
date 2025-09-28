using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using IntervalToast.Domain.Entities;
using IntervalToast.Infrastructure.Interop;

namespace IntervalToast.Infrastructure.Services
{
    /// <summary>
    /// Core singleton service for managing global hotkeys in the IntervalToast application.
    /// Provides comprehensive hotkey registration, management, and integration with the notification system.
    /// </summary>
    public sealed class GlobalHotkeyManager : IDisposable
    {
        #region Singleton Pattern

        private static readonly Lazy<GlobalHotkeyManager> _instance = new(() => new GlobalHotkeyManager());

        /// <summary>
        /// Gets the singleton instance of the GlobalHotkeyManager
        /// </summary>
        public static GlobalHotkeyManager Instance => _instance.Value;

        #endregion

        #region Fields

        private readonly ConcurrentDictionary<int, HotkeyConfiguration> _registeredHotkeys = new();
        private readonly object _lockObject = new();
        private readonly System.Threading.Timer _retryTimer;
        private readonly Queue<HotkeyConfiguration> _retryQueue = new();
        private HotkeySystemConfiguration _systemConfiguration = new();
        private HwndSource? _hotkeyWindowSource;
        private IntPtr _windowHandle = IntPtr.Zero;
        private bool _disposed = false;
        private bool _isEnabled = true;
        private int _nextHotkeyId = 1000; // Start with high number to avoid conflicts

        #endregion

        #region Events

        /// <summary>
        /// Raised when a hotkey is successfully pressed
        /// </summary>
        public event EventHandler<HotkeyEventArgs>? HotkeyPressed;

        /// <summary>
        /// Raised when a hotkey is successfully registered
        /// </summary>
        public event EventHandler<HotkeyEventArgs>? HotkeyRegistered;

        /// <summary>
        /// Raised when a hotkey is successfully unregistered
        /// </summary>
        public event EventHandler<HotkeyEventArgs>? HotkeyUnregistered;

        /// <summary>
        /// Raised when hotkey registration fails
        /// </summary>
        public event EventHandler<HotkeyRegistrationFailedEventArgs>? HotkeyRegistrationFailed;

        /// <summary>
        /// Raised when the global hotkey system is enabled or disabled
        /// </summary>
        public event EventHandler<HotkeySystemStateChangedEventArgs>? SystemStateChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the GlobalHotkeyManager class
        /// </summary>
        private GlobalHotkeyManager()
        {
            // Initialize retry timer for failed registrations
            _retryTimer = new System.Threading.Timer(ProcessRetryQueue, null, Timeout.Infinite, Timeout.Infinite);

            // Load configuration
            LoadConfiguration();

            // Initialize window message handling
            InitializeWindowMessageHandling();

            // Register default hotkeys if enabled
            if (_systemConfiguration.EnableGlobalHotkeys)
            {
                RegisterDefaultHotkeys();
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets whether the global hotkey system is currently enabled
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnSystemStateChanged();

                    if (_isEnabled)
                    {
                        ReregisterAllHotkeys();
                    }
                    else
                    {
                        UnregisterAllHotkeys();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the current system configuration
        /// </summary>
        public HotkeySystemConfiguration SystemConfiguration => _systemConfiguration.Clone();

        /// <summary>
        /// Gets the number of currently registered hotkeys
        /// </summary>
        public int RegisteredHotkeyCount => _registeredHotkeys.Count;

        /// <summary>
        /// Gets whether the window handle is available for registration
        /// </summary>
        public bool IsWindowHandleAvailable => _windowHandle != IntPtr.Zero;

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a global hotkey with the system
        /// </summary>
        /// <param name="hotkeyConfig">The hotkey configuration to register</param>
        /// <returns>True if registration succeeds, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when hotkeyConfig is null</exception>
        public bool RegisterHotkey(HotkeyConfiguration hotkeyConfig)
        {
            if (hotkeyConfig == null)
                throw new ArgumentNullException(nameof(hotkeyConfig));

            if (!_isEnabled || !_systemConfiguration.EnableGlobalHotkeys)
            {
                LogMessage($"Hotkey registration skipped - system disabled: {hotkeyConfig.DisplayName}");
                return false;
            }

            if (!hotkeyConfig.IsValid)
            {
                LogMessage($"Invalid hotkey configuration: {hotkeyConfig.DisplayName}");
                return false;
            }

            // Check if already registered
            if (_registeredHotkeys.Values.Any(h => h.IsSameKeyCombo(hotkeyConfig)))
            {
                LogMessage($"Hotkey already registered: {hotkeyConfig.DisplayName}");
                return false;
            }

            // Validate hotkey combination
            var validation = Win32HotkeyHelper.ValidateHotkey(hotkeyConfig);
            if (!validation.IsValid)
            {
                LogMessage($"Hotkey validation failed: {hotkeyConfig.DisplayName} - {validation.ErrorMessage}");
                return false;
            }

            // Show warnings if any
            if (validation.HasWarnings && _systemConfiguration.ShowHotkeyErrors)
            {
                foreach (var warning in validation.Warnings)
                {
                    ShowHotkeyWarning($"Hotkey warning for '{hotkeyConfig.DisplayName}': {warning}");
                }
            }

            lock (_lockObject)
            {
                try
                {
                    // Assign unique ID if not set
                    if (hotkeyConfig.Id <= 0)
                    {
                        hotkeyConfig.Id = GetNextHotkeyId();
                    }

                    // Attempt registration
                    if (_windowHandle != IntPtr.Zero)
                    {
                        bool success = Win32HotkeyHelper.RegisterHotkey(_windowHandle, hotkeyConfig);
                        if (success)
                        {
                            _registeredHotkeys[hotkeyConfig.Id] = hotkeyConfig;
                            LogMessage($"Hotkey registered successfully: {hotkeyConfig.DisplayName}");
                            HotkeyRegistered?.Invoke(this, new HotkeyEventArgs(hotkeyConfig));
                            return true;
                        }
                    }

                    // Registration failed, add to retry queue if enabled
                    if (_systemConfiguration.AutoRetryFailedRegistrations)
                    {
                        AddToRetryQueue(hotkeyConfig);
                    }

                    return false;
                }
                catch (HotkeyRegistrationException ex)
                {
                    LogMessage($"Hotkey registration error: {hotkeyConfig.DisplayName} - {ex.Message}");

                    if (_systemConfiguration.ShowHotkeyErrors)
                    {
                        ShowHotkeyError($"Failed to register hotkey '{hotkeyConfig.DisplayName}': {ex.Message}");
                    }

                    HotkeyRegistrationFailed?.Invoke(this, new HotkeyRegistrationFailedEventArgs(hotkeyConfig, ex));

                    // Add to retry queue if enabled and error is retryable
                    if (_systemConfiguration.AutoRetryFailedRegistrations && IsRetryableError(ex))
                    {
                        AddToRetryQueue(hotkeyConfig);
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    LogMessage($"Unexpected error registering hotkey: {hotkeyConfig.DisplayName} - {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Unregisters a specific hotkey
        /// </summary>
        /// <param name="hotkeyConfig">The hotkey configuration to unregister</param>
        /// <returns>True if unregistration succeeds, false otherwise</returns>
        public bool UnregisterHotkey(HotkeyConfiguration hotkeyConfig)
        {
            if (hotkeyConfig == null) return false;

            lock (_lockObject)
            {
                if (!_registeredHotkeys.TryGetValue(hotkeyConfig.Id, out var existingHotkey))
                {
                    return true; // Already unregistered
                }

                try
                {
                    bool success = Win32HotkeyHelper.UnregisterHotkey(_windowHandle, existingHotkey);
                    if (success)
                    {
                        _registeredHotkeys.TryRemove(hotkeyConfig.Id, out _);
                        LogMessage($"Hotkey unregistered successfully: {existingHotkey.DisplayName}");
                        HotkeyUnregistered?.Invoke(this, new HotkeyEventArgs(existingHotkey));
                    }
                    return success;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error unregistering hotkey: {existingHotkey.DisplayName} - {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Unregisters all hotkeys
        /// </summary>
        /// <returns>List of hotkeys that failed to unregister</returns>
        public List<HotkeyConfiguration> UnregisterAllHotkeys()
        {
            var failedUnregistrations = new List<HotkeyConfiguration>();

            lock (_lockObject)
            {
                var hotkeysToUnregister = _registeredHotkeys.Values.ToList();

                foreach (var hotkey in hotkeysToUnregister)
                {
                    if (!UnregisterHotkey(hotkey))
                    {
                        failedUnregistrations.Add(hotkey);
                    }
                }

                // Clear retry queue
                _retryQueue.Clear();
            }

            LogMessage($"Unregistered all hotkeys. Failed: {failedUnregistrations.Count}");
            return failedUnregistrations;
        }

        /// <summary>
        /// Gets all currently registered hotkeys
        /// </summary>
        /// <returns>Read-only list of registered hotkey configurations</returns>
        public IReadOnlyList<HotkeyConfiguration> GetRegisteredHotkeys()
        {
            return _registeredHotkeys.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Checks if a specific hotkey combination is available
        /// </summary>
        /// <param name="hotkeyConfig">The hotkey configuration to test</param>
        /// <returns>True if the hotkey can be registered, false if it's already in use</returns>
        public bool IsHotkeyAvailable(HotkeyConfiguration hotkeyConfig)
        {
            if (hotkeyConfig == null || !hotkeyConfig.IsValid) return false;

            // Check if already registered locally
            if (_registeredHotkeys.Values.Any(h => h.IsSameKeyCombo(hotkeyConfig)))
                return false;

            // Check system availability if window handle is available
            if (_windowHandle != IntPtr.Zero)
            {
                return Win32HotkeyHelper.IsHotkeyAvailable(_windowHandle, hotkeyConfig);
            }

            return true;
        }

        /// <summary>
        /// Sets the system configuration
        /// </summary>
        /// <param name="configuration">The new configuration</param>
        public void SetSystemConfiguration(HotkeySystemConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var oldEnabled = _systemConfiguration.EnableGlobalHotkeys;
            _systemConfiguration = configuration.Clone();

            // Save configuration
            SaveConfiguration();

            // Handle enable/disable state change
            if (oldEnabled != _systemConfiguration.EnableGlobalHotkeys)
            {
                IsEnabled = _systemConfiguration.EnableGlobalHotkeys;
            }

            // Update retry timer interval
            if (_systemConfiguration.AutoRetryFailedRegistrations)
            {
                _retryTimer.Change(TimeSpan.FromMilliseconds(_systemConfiguration.RetryDelayMs),
                                 TimeSpan.FromMilliseconds(_systemConfiguration.RetryDelayMs));
            }
            else
            {
                _retryTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }

            LogMessage("System configuration updated");
        }

        /// <summary>
        /// Registers default hotkeys for common actions
        /// </summary>
        public void RegisterDefaultHotkeys()
        {
            if (!_isEnabled || !_systemConfiguration.EnableGlobalHotkeys) return;

            var defaultHotkeys = new List<HotkeyConfiguration>
            {
                CreateHotkeyConfiguration(
                    ModifierKeys.Control | ModifierKeys.Shift, Key.D,
                    "Dismiss All Notifications", () => DismissAllNotifications()),

                CreateHotkeyConfiguration(
                    ModifierKeys.Control | ModifierKeys.Shift, Key.Escape,
                    "Dismiss Latest Notification", () => DismissLatestNotification()),

                CreateHotkeyConfiguration(
                    ModifierKeys.Control | ModifierKeys.Shift, Key.P,
                    "Toggle Notification System", () => ToggleNotificationSystem()),

                CreateHotkeyConfiguration(
                    ModifierKeys.Control | ModifierKeys.Shift, Key.Q,
                    "Show Queue Status", () => ShowQueueStatus())
            };

            foreach (var hotkey in defaultHotkeys)
            {
                RegisterHotkey(hotkey);
            }

            LogMessage($"Registered {defaultHotkeys.Count} default hotkeys");
        }

        /// <summary>
        /// Forces re-registration of all hotkeys (useful after system changes)
        /// </summary>
        public void ReregisterAllHotkeys()
        {
            if (!_isEnabled || !_systemConfiguration.EnableGlobalHotkeys) return;

            var hotkeysToReregister = _registeredHotkeys.Values.ToList();

            // Unregister all first
            UnregisterAllHotkeys();

            // Re-register each hotkey
            foreach (var hotkey in hotkeysToReregister)
            {
                RegisterHotkey(hotkey);
            }

            LogMessage($"Re-registered {hotkeysToReregister.Count} hotkeys");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes window message handling for WM_HOTKEY messages
        /// </summary>
        private void InitializeWindowMessageHandling()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    // Create a hidden window for receiving hotkey messages
                    var window = System.Windows.Application.Current.MainWindow ?? new Window();
                    var windowInteropHelper = new WindowInteropHelper(window);

                    // Ensure the window handle is created
                    if (windowInteropHelper.Handle == IntPtr.Zero)
                    {
                        windowInteropHelper.EnsureHandle();
                    }

                    _windowHandle = windowInteropHelper.Handle;

                    // Hook into the window's message processing
                    _hotkeyWindowSource = HwndSource.FromHwnd(_windowHandle);
                    if (_hotkeyWindowSource != null)
                    {
                        _hotkeyWindowSource.AddHook(WndProc);
                        LogMessage("Window message handling initialized successfully");
                    }
                    else
                    {
                        LogMessage("Failed to initialize window message handling");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error initializing window message handling: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Window procedure for processing WM_HOTKEY messages
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Win32HotkeyHelper.WM_HOTKEY && _isEnabled)
            {
                int hotkeyId = wParam.ToInt32();

                if (_registeredHotkeys.TryGetValue(hotkeyId, out var hotkey))
                {
                    try
                    {
                        var eventArgs = new HotkeyEventArgs(hotkey);
                        HotkeyPressed?.Invoke(this, eventArgs);

                        if (!eventArgs.Handled)
                        {
                            // Execute the hotkey action
                            hotkey.Action?.Invoke();
                            LogMessage($"Hotkey executed: {hotkey.DisplayName}");
                        }

                        handled = true;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error executing hotkey action: {hotkey.DisplayName} - {ex.Message}");
                    }
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Creates a hotkey configuration with action
        /// </summary>
        private HotkeyConfiguration CreateHotkeyConfiguration(ModifierKeys modifiers, Key key, string description, Action action)
        {
            return new HotkeyConfiguration
            {
                Id = GetNextHotkeyId(),
                ModifierKeys = modifiers,
                Key = key,
                Description = description,
                Action = action,
                IsEnabled = true
            };
        }

        /// <summary>
        /// Gets the next available hotkey ID
        /// </summary>
        private int GetNextHotkeyId()
        {
            return Interlocked.Increment(ref _nextHotkeyId);
        }

        /// <summary>
        /// Adds a hotkey to the retry queue
        /// </summary>
        private void AddToRetryQueue(HotkeyConfiguration hotkeyConfig)
        {
            lock (_retryQueue)
            {
                // Don't add duplicates
                if (!_retryQueue.Any(h => h.IsSameKeyCombo(hotkeyConfig)))
                {
                    _retryQueue.Enqueue(hotkeyConfig);

                    // Start retry timer if not already running
                    _retryTimer.Change(TimeSpan.FromMilliseconds(_systemConfiguration.RetryDelayMs),
                                     TimeSpan.FromMilliseconds(_systemConfiguration.RetryDelayMs));

                    LogMessage($"Added hotkey to retry queue: {hotkeyConfig.DisplayName}");
                }
            }
        }

        /// <summary>
        /// Processes the retry queue for failed hotkey registrations
        /// </summary>
        private void ProcessRetryQueue(object? state)
        {
            if (_disposed || !_isEnabled) return;

            lock (_retryQueue)
            {
                var retryCount = 0;
                var maxRetries = Math.Min(_retryQueue.Count, _systemConfiguration.MaxRetryAttempts);

                while (_retryQueue.Count > 0 && retryCount < maxRetries)
                {
                    var hotkey = _retryQueue.Dequeue();

                    try
                    {
                        if (RegisterHotkey(hotkey))
                        {
                            LogMessage($"Retry registration successful: {hotkey.DisplayName}");
                        }
                        else
                        {
                            // If we haven't exceeded max attempts, re-queue
                            if (retryCount < _systemConfiguration.MaxRetryAttempts - 1)
                            {
                                _retryQueue.Enqueue(hotkey);
                            }
                            else
                            {
                                LogMessage($"Max retry attempts reached for hotkey: {hotkey.DisplayName}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error during retry registration: {hotkey.DisplayName} - {ex.Message}");
                    }

                    retryCount++;
                }

                // Stop timer if queue is empty
                if (_retryQueue.Count == 0)
                {
                    _retryTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        /// <summary>
        /// Determines if an error is retryable
        /// </summary>
        private static bool IsRetryableError(HotkeyRegistrationException ex)
        {
            // Retry for certain error codes that might be temporary
            return ex.Win32ErrorCode == Win32HotkeyHelper.ErrorCodes.ERROR_NOT_ENOUGH_MEMORY ||
                   ex.Win32ErrorCode == Win32HotkeyHelper.ErrorCodes.ERROR_ACCESS_DENIED;
        }

        /// <summary>
        /// Loads configuration from file
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                var configPath = GetConfigurationFilePath();
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<HotkeySystemConfiguration>(json);
                    if (config != null)
                    {
                        _systemConfiguration = config;
                        LogMessage("Configuration loaded successfully");
                    }
                }
                else
                {
                    // Create default configuration file
                    SaveConfiguration();
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error loading configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves configuration to file
        /// </summary>
        private void SaveConfiguration()
        {
            try
            {
                var configPath = GetConfigurationFilePath();
                var directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_systemConfiguration, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
                LogMessage("Configuration saved successfully");
            }
            catch (Exception ex)
            {
                LogMessage($"Error saving configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the configuration file path
        /// </summary>
        private static string GetConfigurationFilePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "IntervalToast", "hotkey-config.json");
        }

        /// <summary>
        /// Logs a message if logging is enabled
        /// </summary>
        private void LogMessage(string message)
        {
            if (_systemConfiguration.EnableLogging)
            {
                System.Diagnostics.Debug.WriteLine($"[GlobalHotkeyManager] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}");
            }
        }

        /// <summary>
        /// Shows a hotkey error notification
        /// </summary>
        private static void ShowHotkeyError(string message)
        {
            var data = NotificationData.CreateError("Hotkey Error", message);
            NotificationManager.Instance.ShowNotification(data);
        }

        /// <summary>
        /// Shows a hotkey warning notification
        /// </summary>
        private static void ShowHotkeyWarning(string message)
        {
            var data = NotificationData.CreateWarning("Hotkey Warning", message);
            NotificationManager.Instance.ShowNotification(data);
        }

        /// <summary>
        /// Raises the system state changed event
        /// </summary>
        private void OnSystemStateChanged()
        {
            SystemStateChanged?.Invoke(this, new HotkeySystemStateChangedEventArgs(_isEnabled));
        }

        #endregion

        #region Hotkey Actions

        /// <summary>
        /// Dismisses all notifications using the hotkey action handler
        /// </summary>
        private void DismissAllNotifications()
        {
            try
            {
                var result = HotkeyActionHandler.Instance.ExecuteAction(HotkeyAction.DismissAll);
                LogActionResult("DismissAll", result);
            }
            catch (Exception ex)
            {
                LogMessage($"Error executing DismissAll action: {ex.Message}");
                // Fallback to direct action
                NotificationManager.Instance.CloseAllNotifications();
            }
        }

        /// <summary>
        /// Dismisses the latest notification using the hotkey action handler
        /// </summary>
        private void DismissLatestNotification()
        {
            try
            {
                var result = HotkeyActionHandler.Instance.ExecuteAction(HotkeyAction.DismissLatest);
                LogActionResult("DismissLatest", result);
            }
            catch (Exception ex)
            {
                LogMessage($"Error executing DismissLatest action: {ex.Message}");
                // Fallback to direct action
                var activeNotifications = NotificationManager.Instance.GetActiveNotifications();
                if (activeNotifications.Count > 0)
                {
                    var latest = activeNotifications.OrderByDescending(n => n.CreatedAt).First();
                    NotificationManager.Instance.CloseNotification(latest.Id);
                }
            }
        }

        /// <summary>
        /// Toggles the notification system using the hotkey action handler
        /// </summary>
        private void ToggleNotificationSystem()
        {
            try
            {
                var result = HotkeyActionHandler.Instance.ExecuteAction(HotkeyAction.ToggleSystem);
                LogActionResult("ToggleSystem", result);
            }
            catch (Exception ex)
            {
                LogMessage($"Error executing ToggleSystem action: {ex.Message}");
                // Fallback to showing status
                var status = NotificationManager.Instance.GetQueueStatus();
                var message = status.ActiveCount > 0 ? "Notifications are active" : "No active notifications";
                var data = NotificationData.CreateInfo("System Status", message);
                NotificationManager.Instance.ShowNotification(data);
            }
        }

        /// <summary>
        /// Shows the current queue status using the hotkey action handler
        /// </summary>
        private void ShowQueueStatus()
        {
            try
            {
                var result = HotkeyActionHandler.Instance.ExecuteAction(HotkeyAction.ShowQueueStatus);
                LogActionResult("ShowQueueStatus", result);
            }
            catch (Exception ex)
            {
                LogMessage($"Error executing ShowQueueStatus action: {ex.Message}");
                // Fallback to direct status display
                var status = NotificationManager.Instance.GetQueueStatus();
                var message = $"Active: {status.ActiveCount}/{status.MaxVisible}\n" +
                             $"Pending: {status.PendingCount}/{status.MaxQueue}\n" +
                             $"Total: {status.TotalCount}\n" +
                             $"Overflowing: {(status.IsOverflowing ? "Yes" : "No")}";
                var data = NotificationData.CreateSystem("Queue Status", message);
                NotificationManager.Instance.ShowNotification(data);
            }
        }

        /// <summary>
        /// Logs the result of a hotkey action execution
        /// </summary>
        private void LogActionResult(string actionName, HotkeyActionResult result)
        {
            var message = $"Hotkey action '{actionName}' executed - Success: {result.Success}, " +
                         $"ActionPerformed: {result.ActionPerformed}, ItemsAffected: {result.ItemsAffected}, " +
                         $"Message: {result.Message}";

            LogMessage(message);

            if (!result.Success && result.Error != null)
            {
                LogMessage($"Error details: {result.Error}");
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of the GlobalHotkeyManager resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // Unregister all hotkeys
                UnregisterAllHotkeys();

                // Clean up timer
                _retryTimer?.Dispose();

                // Clean up window source
                _hotkeyWindowSource?.RemoveHook(WndProc);
                _hotkeyWindowSource?.Dispose();

                // Save configuration
                SaveConfiguration();

                _disposed = true;
                LogMessage("GlobalHotkeyManager disposed");
            }
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Event arguments for hotkey registration failures
    /// </summary>
    public class HotkeyRegistrationFailedEventArgs : EventArgs
    {
        /// <summary>
        /// The hotkey configuration that failed to register
        /// </summary>
        public HotkeyConfiguration Hotkey { get; }

        /// <summary>
        /// The exception that caused the failure
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Timestamp when the failure occurred
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.Now;

        /// <summary>
        /// Initializes new hotkey registration failed event arguments
        /// </summary>
        public HotkeyRegistrationFailedEventArgs(HotkeyConfiguration hotkey, Exception exception)
        {
            Hotkey = hotkey ?? throw new ArgumentNullException(nameof(hotkey));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }
    }

    /// <summary>
    /// Event arguments for system state changes
    /// </summary>
    public class HotkeySystemStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Whether the system is now enabled
        /// </summary>
        public bool IsEnabled { get; }

        /// <summary>
        /// Timestamp when the state changed
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.Now;

        /// <summary>
        /// Initializes new system state changed event arguments
        /// </summary>
        public HotkeySystemStateChangedEventArgs(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }
    }

    /// <summary>
    /// Extension methods for HotkeySystemConfiguration
    /// </summary>
    public static class HotkeySystemConfigurationExtensions
    {
        /// <summary>
        /// Creates a copy of the hotkey system configuration
        /// </summary>
        public static HotkeySystemConfiguration Clone(this HotkeySystemConfiguration config)
        {
            return new HotkeySystemConfiguration
            {
                EnableGlobalHotkeys = config.EnableGlobalHotkeys,
                ShowHotkeyErrors = config.ShowHotkeyErrors,
                MaxHotkeys = config.MaxHotkeys,
                AutoRetryFailedRegistrations = config.AutoRetryFailedRegistrations,
                RetryDelayMs = config.RetryDelayMs,
                MaxRetryAttempts = config.MaxRetryAttempts,
                EnableLogging = config.EnableLogging,
                AllowNoModifierKeys = config.AllowNoModifierKeys
            };
        }
    }

    #endregion
}