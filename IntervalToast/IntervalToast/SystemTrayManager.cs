using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace IntervalToast
{
    /// <summary>
    /// Manages system tray integration for background operation and quick access controls
    /// </summary>
    public class SystemTrayManager : IDisposable
    {
        #region Fields

        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private readonly NotificationManager _notificationManager;
        private readonly GlobalHotkeyManager _hotkeyManager;
        private readonly SettingsManager _settingsManager;
        private bool _disposed = false;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the user requests to show the configuration window
        /// </summary>
        public event EventHandler? ShowConfigurationRequested;

        /// <summary>
        /// Fired when the user requests to exit the application
        /// </summary>
        public event EventHandler? ExitApplicationRequested;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the SystemTrayManager
        /// </summary>
        /// <param name="notificationManager">The notification manager instance</param>
        /// <param name="hotkeyManager">The global hotkey manager instance</param>
        /// <param name="settingsManager">The settings manager instance</param>
        public SystemTrayManager(NotificationManager notificationManager, GlobalHotkeyManager hotkeyManager, SettingsManager settingsManager)
        {
            _notificationManager = notificationManager ?? throw new ArgumentNullException(nameof(notificationManager));
            _hotkeyManager = hotkeyManager ?? throw new ArgumentNullException(nameof(hotkeyManager));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));

            InitializeSystemTray();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets whether the system tray icon is visible
        /// </summary>
        public bool IsVisible
        {
            get => _notifyIcon?.Visible ?? false;
            set
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the tooltip text for the system tray icon
        /// </summary>
        public string ToolTipText
        {
            get => _notifyIcon?.Text ?? string.Empty;
            set
            {
                if (_notifyIcon != null && !string.IsNullOrWhiteSpace(value))
                {
                    _notifyIcon.Text = value.Length > 63 ? value.Substring(0, 63) : value;
                }
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the system tray icon and context menu
        /// </summary>
        private void InitializeSystemTray()
        {
            try
            {
                // Create the system tray icon
                _notifyIcon = new NotifyIcon
                {
                    Icon = GetApplicationIcon(),
                    Text = "IntervalToast - Notification System",
                    Visible = true
                };

                // Create context menu
                CreateContextMenu();

                // Wire up events
                _notifyIcon.DoubleClick += OnTrayIconDoubleClick;
                _notifyIcon.MouseClick += OnTrayIconMouseClick;

                // Subscribe to notification manager events for tray updates
                _notificationManager.NotificationShown += OnNotificationShown;
                _notificationManager.NotificationClosed += OnNotificationDismissed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize system tray: {ex.Message}");
                // Don't throw - system tray is optional functionality
            }
        }

        /// <summary>
        /// Creates the context menu for the system tray icon
        /// </summary>
        private void CreateContextMenu()
        {
            _contextMenu = new ContextMenuStrip();

            // Quick Actions Section
            var quickTestItem = new ToolStripMenuItem("Quick Test Notification")
            {
                Image = GetEmbeddedIcon("test")
            };
            quickTestItem.Click += (s, e) => ShowQuickTestNotification();
            _contextMenu.Items.Add(quickTestItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // System Control Section
            var dismissAllItem = new ToolStripMenuItem("Dismiss All Notifications")
            {
                Image = GetEmbeddedIcon("dismiss")
            };
            dismissAllItem.Click += (s, e) => _notificationManager.CloseAllNotifications();
            _contextMenu.Items.Add(dismissAllItem);

            var pauseResumeItem = new ToolStripMenuItem("Pause Notifications")
            {
                Image = GetEmbeddedIcon("pause")
            };
            pauseResumeItem.Click += OnPauseResumeClick;
            _contextMenu.Items.Add(pauseResumeItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // Queue Status Section
            var queueStatusItem = new ToolStripMenuItem("Show Queue Status")
            {
                Image = GetEmbeddedIcon("status")
            };
            queueStatusItem.Click += (s, e) => {
                var status = _notificationManager.GetEnhancedQueueStatus();
                var statusMessage = status.GenerateDetailedStatusMessage();
                _notificationManager.ShowSystemFeedback("Queue Status", statusMessage);
            };
            _contextMenu.Items.Add(queueStatusItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // Configuration Section
            var configItem = new ToolStripMenuItem("Configuration Dashboard")
            {
                Image = GetEmbeddedIcon("config"),
                Font = new Font(_contextMenu.Font, System.Drawing.FontStyle.Bold)
            };
            configItem.Click += (s, e) => ShowConfigurationRequested?.Invoke(this, EventArgs.Empty);
            _contextMenu.Items.Add(configItem);

            var settingsItem = new ToolStripMenuItem("Settings")
            {
                Image = GetEmbeddedIcon("settings")
            };
            settingsItem.Click += (s, e) => ShowConfigurationRequested?.Invoke(this, EventArgs.Empty);
            _contextMenu.Items.Add(settingsItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // About and Exit Section
            var aboutItem = new ToolStripMenuItem("About IntervalToast")
            {
                Image = GetEmbeddedIcon("about")
            };
            aboutItem.Click += OnAboutClick;
            _contextMenu.Items.Add(aboutItem);

            var exitItem = new ToolStripMenuItem("Exit")
            {
                Image = GetEmbeddedIcon("exit")
            };
            exitItem.Click += (s, e) => ExitApplicationRequested?.Invoke(this, EventArgs.Empty);
            _contextMenu.Items.Add(exitItem);

            // Update context menu before showing
            _contextMenu.Opening += OnContextMenuOpening;

            // Assign context menu to the notify icon
            _notifyIcon.ContextMenuStrip = _contextMenu;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles double-click on the system tray icon
        /// </summary>
        private void OnTrayIconDoubleClick(object? sender, EventArgs e)
        {
            ShowConfigurationRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Handles mouse clicks on the system tray icon
        /// </summary>
        private void OnTrayIconMouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Left click shows a quick status notification
                ShowQuickStatusNotification();
            }
        }

        /// <summary>
        /// Updates context menu items before showing the menu
        /// </summary>
        private void OnContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_contextMenu == null) return;

            try
            {
                // Update pause/resume text
                var pauseResumeItem = _contextMenu.Items["pauseResumeItem"] as ToolStripMenuItem;
                if (pauseResumeItem != null)
                {
                    var status = _notificationManager.GetEnhancedQueueStatus();
                var isPaused = !status.SystemEnabled;
                    pauseResumeItem.Text = isPaused ? "Resume Notifications" : "Pause Notifications";
                    pauseResumeItem.Image = GetEmbeddedIcon(isPaused ? "resume" : "pause");
                }

                // Update dismiss all item availability
                var dismissAllItem = _contextMenu.Items.OfType<ToolStripMenuItem>()
                    .FirstOrDefault(item => item.Text.Contains("Dismiss All"));
                if (dismissAllItem != null)
                {
                    var status = _notificationManager.GetEnhancedQueueStatus();
                    dismissAllItem.Enabled = status.ActiveCount > 0;
                }

                // Update queue status item
                var queueStatusItem = _contextMenu.Items.OfType<ToolStripMenuItem>()
                    .FirstOrDefault(item => item.Text.Contains("Queue Status"));
                if (queueStatusItem != null)
                {
                    var status = _notificationManager.GetEnhancedQueueStatus();
                    queueStatusItem.Text = $"Queue Status ({status.ActiveCount} active, {status.PendingCount} queued)";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating context menu: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles pause/resume button clicks
        /// </summary>
        private void OnPauseResumeClick(object? sender, EventArgs e)
        {
            try
            {
                // Note: NotificationManager doesn't have direct Enable/Disable methods
                // This is a placeholder for future implementation
                ShowTrayBalloon("Feature Not Available", "Pause/Resume functionality will be added in a future update", ToolTipIcon.Info, 3000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling notification state: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the about dialog
        /// </summary>
        private void OnAboutClick(object? sender, EventArgs e)
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
                var aboutMessage = $"IntervalToast Notification System\n\n" +
                                  $"Version: {version}\n" +
                                  $"A customizable desktop notification system\n" +
                                  $"with advanced stacking and management features.\n\n" +
                                  $"Features:\n" +
                                  $"• Custom notification themes and animations\n" +
                                  $"• Advanced stacking and queue management\n" +
                                  $"• Global hotkey integration\n" +
                                  $"• Category and priority system\n" +
                                  $"• System tray integration\n\n" +
                                  $"© 2024 IntervalToast Project";

                MessageBox.Show(aboutMessage, "About IntervalToast", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing about dialog: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles notification shown events for tray updates
        /// </summary>
        private void OnNotificationShown(object? sender, NotificationEventArgs e)
        {
            UpdateTrayTooltip();
        }

        /// <summary>
        /// Handles notification dismissed events for tray updates
        /// </summary>
        private void OnNotificationDismissed(object? sender, NotificationEventArgs e)
        {
            UpdateTrayTooltip();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows a balloon tooltip from the system tray icon
        /// </summary>
        /// <param name="title">The balloon title</param>
        /// <param name="text">The balloon text</param>
        /// <param name="icon">The balloon icon</param>
        /// <param name="timeout">The display timeout in milliseconds</param>
        public void ShowTrayBalloon(string title, string text, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 3000)
        {
            try
            {
                _notifyIcon?.ShowBalloonTip(timeout, title, text, icon);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to show tray balloon: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the tray icon and tooltip based on current state
        /// </summary>
        public void UpdateTrayTooltip()
        {
            try
            {
                if (_notifyIcon == null) return;

                var status = _notificationManager.GetEnhancedQueueStatus();
                var activeCount = status.ActiveCount;
                var queueCount = status.PendingCount;
                var isEnabled = status.SystemEnabled;

                var statusText = "IntervalToast";

                if (!isEnabled)
                {
                    statusText += " (Paused)";
                }
                else if (activeCount > 0 || queueCount > 0)
                {
                    statusText += $" - {activeCount} active";
                    if (queueCount > 0)
                    {
                        statusText += $", {queueCount} queued";
                    }
                }
                else
                {
                    statusText += " - Ready";
                }

                ToolTipText = statusText;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating tray tooltip: {ex.Message}");
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Gets the application icon for the system tray
        /// </summary>
        private Icon GetApplicationIcon()
        {
            try
            {
                // Try to get icon from current application
                var appIcon = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
                if (appIcon != null) return appIcon;

                // Fallback to creating a simple icon
                return CreateDefaultIcon();
            }
            catch
            {
                return CreateDefaultIcon();
            }
        }

        /// <summary>
        /// Creates a default icon when no application icon is available
        /// </summary>
        private Icon CreateDefaultIcon()
        {
            try
            {
                using var bitmap = new Bitmap(16, 16);
                using var graphics = Graphics.FromImage(bitmap);

                // Create a simple notification-style icon
                graphics.Clear(Color.Transparent);
                graphics.FillEllipse(Brushes.DodgerBlue, 2, 2, 12, 12);
                graphics.FillEllipse(Brushes.White, 4, 4, 8, 8);
                graphics.FillEllipse(Brushes.DodgerBlue, 6, 6, 4, 4);

                return Icon.FromHandle(bitmap.GetHicon());
            }
            catch
            {
                // Ultimate fallback - use system information icon
                return SystemIcons.Information;
            }
        }

        /// <summary>
        /// Gets embedded icons for menu items
        /// </summary>
        private Image? GetEmbeddedIcon(string iconName)
        {
            try
            {
                // In a real implementation, you would embed icon resources
                // For now, return null to use default system icons
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Shows a quick test notification
        /// </summary>
        private void ShowQuickTestNotification()
        {
            try
            {
                var notification = NotificationData.CreateInfo(
                    "Quick Test",
                    $"Test notification from system tray at {DateTime.Now:HH:mm:ss}"
                );

                _notificationManager.ShowNotification(notification);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to show quick test notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows a quick status notification
        /// </summary>
        private void ShowQuickStatusNotification()
        {
            try
            {
                var status = _notificationManager.GetEnhancedQueueStatus();
                var activeCount = status.ActiveCount;
                var queueCount = status.PendingCount;
                var isEnabled = status.SystemEnabled;

                string title = "IntervalToast Status";
                string message;

                if (!isEnabled)
                {
                    message = "Notification system is currently paused";
                }
                else if (activeCount == 0 && queueCount == 0)
                {
                    message = "System ready - No active notifications";
                }
                else
                {
                    message = $"Active: {activeCount}, Queued: {queueCount}";
                }

                var notification = NotificationData.CreateSystem(title, message);
                notification.CustomTimeout = TimeSpan.FromSeconds(3);

                _notificationManager.ShowNotification(notification);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to show status notification: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the system tray manager and cleans up resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    // Unsubscribe from events
                    if (_notificationManager != null)
                    {
                        _notificationManager.NotificationShown -= OnNotificationShown;
                        _notificationManager.NotificationClosed -= OnNotificationDismissed;
                    }

                    // Dispose system tray components
                    if (_notifyIcon != null)
                    {
                        _notifyIcon.Visible = false;
                        _notifyIcon.Dispose();
                        _notifyIcon = null;
                    }

                    _contextMenu?.Dispose();
                    _contextMenu = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error disposing SystemTrayManager: {ex.Message}");
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~SystemTrayManager()
        {
            Dispose(false);
        }

        #endregion
    }

}