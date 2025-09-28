using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace IntervalToast
{
    /// <summary>
    /// Main configuration dashboard for IntervalToast notification system
    /// Phase 5 implementation featuring comprehensive configuration interface
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private readonly NotificationManager _notificationManager;
        private readonly GlobalHotkeyManager _hotkeyManager;
        private readonly SettingsManager _settingsManager;
        private readonly SystemTrayManager? _systemTrayManager;
        private bool _isMinimizedToTray = false;
        private bool _suppressCloseToTray = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainWindow
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Initialize core managers
            _settingsManager = new SettingsManager();
            _notificationManager = NotificationManager.Instance;
            _hotkeyManager = GlobalHotkeyManager.Instance;

            // Initialize system tray manager
            try
            {
                _systemTrayManager = new SystemTrayManager(_notificationManager, _hotkeyManager, _settingsManager);
                _systemTrayManager.ShowConfigurationRequested += OnShowConfigurationRequested;
                _systemTrayManager.ExitApplicationRequested += OnExitApplicationRequested;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize system tray: {ex.Message}");
            }

            // Load settings and initialize UI
            InitializeConfiguration();

            // Setup event handlers
            Loaded += MainWindow_Loaded;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes configuration and applies settings
        /// </summary>
        private async void InitializeConfiguration()
        {
            try
            {
                // Load settings
                await _settingsManager.LoadSettingsAsync();

                // Apply configuration to notification manager
                var config = _settingsManager.NotificationConfiguration;
                _notificationManager.SetGlobalConfiguration(config);

                // Subscribe to settings events
                _settingsManager.SettingsLoaded += OnSettingsLoaded;
                _settingsManager.SettingsSaved += OnSettingsSaved;
                _settingsManager.SettingChanged += OnSettingChanged;

                // Subscribe to notification manager events
                _notificationManager.NotificationShown += OnNotificationShown;
                _notificationManager.NotificationClosed += OnNotificationDismissed;

                Debug.WriteLine("Configuration initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize configuration: {ex.Message}");
                ShowErrorMessage("Configuration Error", $"Failed to load settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the window loaded event
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize UI with current settings
            UpdateUIFromSettings();
            UpdateSystemStatus();
            RefreshStatistics();
            LoadCategoryData();

            // Show system tray notification
            _systemTrayManager?.ShowTrayBalloon(
                "IntervalToast Ready",
                "Configuration dashboard is ready. System tray icon available.",
                System.Windows.Forms.ToolTipIcon.Info,
                3000);
        }

        #endregion

        #region Window State Management

        /// <summary>
        /// Handles window state changes for tray integration
        /// </summary>
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && _settingsManager.CurrentSettings.ApplicationPreferences.MinimizeToTray)
            {
                MinimizeToSystemTray();
            }
        }

        /// <summary>
        /// Handles window closing to support minimize to tray
        /// </summary>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_suppressCloseToTray && _settingsManager.CurrentSettings.ApplicationPreferences.MinimizeToTray)
            {
                e.Cancel = true;
                MinimizeToSystemTray();
            }
            else
            {
                // Cleanup before exit
                _systemTrayManager?.Dispose();
            }
        }

        /// <summary>
        /// Minimizes the window to system tray
        /// </summary>
        private void MinimizeToSystemTray()
        {
            if (_systemTrayManager != null)
            {
                Hide();
                _isMinimizedToTray = true;
                _systemTrayManager.IsVisible = true;

                if (!_settingsManager.CurrentSettings.ApplicationPreferences.ShowConfigurationOnStartup)
                {
                    _systemTrayManager.ShowTrayBalloon(
                        "IntervalToast",
                        "Application minimized to system tray. Double-click to restore.",
                        System.Windows.Forms.ToolTipIcon.Info,
                        2000);
                }
            }
        }

        /// <summary>
        /// Restores the window from system tray
        /// </summary>
        private void RestoreFromSystemTray()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            _isMinimizedToTray = false;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Shows configuration window when requested from system tray
        /// </summary>
        private void OnShowConfigurationRequested(object? sender, EventArgs e)
        {
            RestoreFromSystemTray();
        }

        /// <summary>
        /// Handles application exit request from system tray
        /// </summary>
        private void OnExitApplicationRequested(object? sender, EventArgs e)
        {
            _suppressCloseToTray = true;
            Close();
        }

        /// <summary>
        /// Handles settings loaded event
        /// </summary>
        private void OnSettingsLoaded(object? sender, SettingsEventArgs e)
        {
            UpdateUIFromSettings();
        }

        /// <summary>
        /// Handles settings saved event
        /// </summary>
        private void OnSettingsSaved(object? sender, SettingsEventArgs e)
        {
            StatusText.Text = $"Settings saved at {DateTime.Now:HH:mm:ss}";
        }

        /// <summary>
        /// Handles individual setting changes
        /// </summary>
        private void OnSettingChanged(object? sender, SettingChangedEventArgs e)
        {
            Debug.WriteLine($"Setting changed: {e.SettingName}");

            // Update notification manager configuration if notification settings changed
            if (e.SettingName == "NotificationConfiguration")
            {
                _notificationManager.SetGlobalConfiguration(_settingsManager.NotificationConfiguration);
            }
        }

        /// <summary>
        /// Handles notification shown events for statistics
        /// </summary>
        private void OnNotificationShown(object? sender, NotificationEventArgs e)
        {
            UpdateSystemStatus();
            UpdateRecentActivity($"Shown: {e.Data.Title}");
        }

        /// <summary>
        /// Handles notification dismissed events for statistics
        /// </summary>
        private void OnNotificationDismissed(object? sender, NotificationEventArgs e)
        {
            UpdateSystemStatus();
            UpdateRecentActivity($"Dismissed: {e.Data.Title}");
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// Updates UI controls from current settings
        /// </summary>
        private void UpdateUIFromSettings()
        {
            try
            {
                var settings = _settingsManager.CurrentSettings;
                var config = settings.NotificationConfiguration;
                var prefs = settings.ApplicationPreferences;

                // Update animation settings
                SetComboBoxSelection(EntryAnimationComboBox, config.EntryAnimation.ToString());
                SetComboBoxSelection(ExitAnimationComboBox, config.ExitAnimation.ToString());
                SetComboBoxSelection(AnimationSpeedComboBox, config.AnimationPreset.ToString());

                // Update system behavior settings
                MaxNotificationsSlider.Value = config.MaxVisibleNotifications;
                AutoCloseDelaySlider.Value = config.AutoCloseDelay.TotalSeconds;
                NotificationSpacingSlider.Value = config.NotificationSpacing;
                EnableHoverPauseCheckBox.IsChecked = config.PauseOnHover;

                // Update application preferences
                StartWithWindowsCheckBox.IsChecked = prefs.StartWithWindows;
                MinimizeToTrayCheckBox.IsChecked = prefs.MinimizeToTray;
                ShowConfigOnStartupCheckBox.IsChecked = prefs.ShowConfigurationOnStartup;
                CollectStatisticsCheckBox.IsChecked = prefs.CollectUsageStatistics;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating UI from settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates system status indicators
        /// </summary>
        private void UpdateSystemStatus()
        {
            try
            {
                var status = _notificationManager.GetEnhancedQueueStatus();

                // Update system status
                NotificationSystemStatusText.Text = status.SystemEnabled ? "System Active" : "System Paused";
                NotificationSystemStatusText.Foreground = status.SystemEnabled ?
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96)) :
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));

                // Update notification counts
                ActiveNotificationsText.Text = $"{status.ActiveCount} Active";
                QueuedNotificationsText.Text = $"{status.PendingCount} Queued";

                // Update toggle button text
                ToggleSystemBtn.Content = status.SystemEnabled ? "Pause System" : "Resume System";

                // Update system tray tooltip
                _systemTrayManager?.UpdateTrayTooltip();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating system status: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates recent activity display
        /// </summary>
        private void UpdateRecentActivity(string activity)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var currentText = RecentActivityText.Text;

                if (currentText == "No recent notifications")
                {
                    RecentActivityText.Text = $"[{timestamp}] {activity}";
                }
                else
                {
                    var lines = currentText.Split('\n').ToList();
                    lines.Insert(0, $"[{timestamp}] {activity}");

                    // Keep only last 5 activities
                    if (lines.Count > 5)
                    {
                        lines = lines.Take(5).ToList();
                    }

                    RecentActivityText.Text = string.Join("\n", lines);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating recent activity: {ex.Message}");
            }
        }

        /// <summary>
        /// Refreshes statistics display
        /// </summary>
        private void RefreshStatistics()
        {
            try
            {
                var stats = _settingsManager.CurrentSettings.Statistics;

                TotalNotificationsText.Text = stats.TotalNotificationsShown.ToString();
                AverageDisplayTimeText.Text = $"{stats.AverageDisplayTime.TotalSeconds:F1}s";
                MostActiveTimeText.Text = stats.MostActiveHour.ToString(@"hh\:mm");

                // Update category breakdown
                CategoryStatsPanel.Children.Clear();
                foreach (var categoryCount in stats.NotificationsByCategory)
                {
                    var panel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                    panel.Children.Add(new TextBlock { Text = $"{categoryCount.Key}:", Width = 80, FontWeight = FontWeights.SemiBold });
                    panel.Children.Add(new TextBlock { Text = categoryCount.Value.ToString(), Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 102, 102)) });
                    CategoryStatsPanel.Children.Add(panel);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing statistics: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads category data into the data grid
        /// </summary>
        private void LoadCategoryData()
        {
            try
            {
                var config = _settingsManager.NotificationConfiguration;
                var categoryData = new List<CategoryDisplayItem>();

                foreach (var category in config.CategoryConfigurations)
                {
                    categoryData.Add(new CategoryDisplayItem
                    {
                        CategoryName = category.Key.ToString(),
                        DefaultTimeout = category.Value.DefaultTimeout.ToString(),
                        AllowAutoDismiss = category.Value.AllowAutoDismiss,
                        IconContent = category.Value.IconContent
                    });
                }

                CategoryDataGrid.ItemsSource = categoryData;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading category data: {ex.Message}");
            }
        }

        #endregion

        #region Button Event Handlers

        // Header buttons
        private void MinimizeToTrayBtn_Click(object sender, RoutedEventArgs e)
        {
            MinimizeToSystemTray();
        }

        private void QuickTestBtn_Click(object sender, RoutedEventArgs e)
        {
            var notification = NotificationData.CreateInfo(
                "Quick Test",
                $"Test notification from dashboard at {DateTime.Now:HH:mm:ss}"
            );
            _notificationManager.ShowNotification(notification);
        }

        // Dashboard tab buttons
        private void DismissAllBtn_Click(object sender, RoutedEventArgs e)
        {
            _notificationManager.CloseAllNotifications();
        }

        private void ShowQueueStatusBtn_Click(object sender, RoutedEventArgs e)
        {
            // Show queue status via system feedback
            var status = _notificationManager.GetEnhancedQueueStatus();
            var statusMessage = status.GenerateDetailedStatusMessage();
            _notificationManager.ShowSystemFeedback("Queue Status", statusMessage);
        }

        private void ToggleSystemBtn_Click(object sender, RoutedEventArgs e)
        {
            var status = _notificationManager.GetEnhancedQueueStatus();
            if (status.SystemEnabled)
            {
                // Note: There's no direct Disable method, so we'll show a notification about this limitation
                ShowNotImplementedMessage("System Pause/Resume");
            }
            else
            {
                ShowNotImplementedMessage("System Pause/Resume");
            }
            UpdateSystemStatus();
        }

        // Quick test buttons
        private void ShowInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            var notification = NotificationData.CreateInfo("Information", "This is an info notification");
            _notificationManager.ShowNotification(notification);
        }

        private void ShowSuccessBtn_Click(object sender, RoutedEventArgs e)
        {
            var notification = NotificationData.CreateSuccess("Success", "Operation completed successfully");
            _notificationManager.ShowNotification(notification);
        }

        private void ShowWarningBtn_Click(object sender, RoutedEventArgs e)
        {
            var notification = NotificationData.CreateWarning("Warning", "Please review this important message");
            _notificationManager.ShowNotification(notification);
        }

        private void ShowErrorBtn_Click(object sender, RoutedEventArgs e)
        {
            var notification = NotificationData.CreateError("Error", "An error has occurred that requires attention");
            _notificationManager.ShowNotification(notification);
        }

        private void ShowSystemBtn_Click(object sender, RoutedEventArgs e)
        {
            var notification = NotificationData.CreateSystem("System", "System status notification");
            _notificationManager.ShowNotification(notification);
        }

        // Theme editor buttons
        private void ApplyLightThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            var lightTheme = NotificationConfiguration.CreateLightTheme();
            ApplyTheme(lightTheme);
        }

        private void ApplyDarkThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            var darkTheme = NotificationConfiguration.CreateDarkTheme();
            ApplyTheme(darkTheme);
        }

        private void ApplyMinimalThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            var minimalTheme = NotificationConfiguration.CreateMinimal();
            ApplyTheme(minimalTheme);
        }

        private void TestLivePreviewBtn_Click(object sender, RoutedEventArgs e)
        {
            var notification = NotificationData.CreateInfo("Theme Preview", "This notification shows the current theme settings");
            _notificationManager.ShowNotification(notification);
        }

        // Statistics buttons
        private void RefreshStatisticsBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshStatistics();
        }

        // Category management buttons
        private void AddCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowNotImplementedMessage("Add Category");
        }

        private void ResetCategoriesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ShowConfirmationDialog("Reset Categories", "Are you sure you want to reset all categories to defaults?"))
            {
                // Reset to defaults
                var config = _settingsManager.NotificationConfiguration;
                config.CategoryConfigurations = CategoryConfiguration.GetDefaults();
                _ = _settingsManager.UpdateNotificationConfigurationAsync(config);
                LoadCategoryData();
            }
        }

        // Advanced settings buttons
        private void TestAnimationBtn_Click(object sender, RoutedEventArgs e)
        {
            var notification = NotificationData.CreateInfo("Animation Test", "Testing current animation settings");
            _notificationManager.ShowNotification(notification);
        }

        // Settings buttons
        private async void ExportSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"IntervalToast_Settings_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                var success = await _settingsManager.ExportSettingsAsync(dialog.FileName);
                if (success)
                {
                    ShowSuccessMessage("Export Complete", $"Settings exported to {dialog.FileName}");
                }
                else
                {
                    ShowErrorMessage("Export Failed", "Failed to export settings");
                }
            }
        }

        private async void ImportSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json"
            };

            if (dialog.ShowDialog() == true)
            {
                var success = await _settingsManager.ImportSettingsAsync(dialog.FileName);
                if (success)
                {
                    ShowSuccessMessage("Import Complete", "Settings imported successfully");
                    UpdateUIFromSettings();
                }
                else
                {
                    ShowErrorMessage("Import Failed", "Failed to import settings");
                }
            }
        }

        private async void ResetSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ShowConfirmationDialog("Reset Settings", "Are you sure you want to reset all settings to defaults?"))
            {
                var success = await _settingsManager.ResetToDefaultsAsync();
                if (success)
                {
                    ShowSuccessMessage("Reset Complete", "All settings have been reset to defaults");
                    UpdateUIFromSettings();
                }
                else
                {
                    ShowErrorMessage("Reset Failed", "Failed to reset settings");
                }
            }
        }

        private async void CreateBackupBtn_Click(object sender, RoutedEventArgs e)
        {
            var success = await _settingsManager.CreateBackupAsync();
            if (success)
            {
                ShowSuccessMessage("Backup Created", "Settings backup created successfully");
            }
            else
            {
                ShowErrorMessage("Backup Failed", "Failed to create settings backup");
            }
        }

        // Footer buttons
        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            var aboutMessage = "IntervalToast Configuration Dashboard\n\n" +
                              "Phase 5: Advanced Configuration & Management Interface\n\n" +
                              "Features:\n" +
                              "• Comprehensive settings management\n" +
                              "• System tray integration\n" +
                              "• Theme editor with live preview\n" +
                              "• Statistics dashboard\n" +
                              "• Category management\n" +
                              "• Advanced configuration options\n\n" +
                              "© 2024 IntervalToast Project";

            System.Windows.MessageBox.Show(aboutMessage, "About IntervalToast", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Applies a theme configuration
        /// </summary>
        private async void ApplyTheme(NotificationConfiguration themeConfig)
        {
            try
            {
                var currentConfig = _settingsManager.NotificationConfiguration;

                // Apply theme colors
                currentConfig.BackgroundColor = themeConfig.BackgroundColor;
                currentConfig.TitleColor = themeConfig.TitleColor;
                currentConfig.MessageColor = themeConfig.MessageColor;
                currentConfig.AccentColor = themeConfig.AccentColor;

                // Apply theme properties
                currentConfig.Width = themeConfig.Width;
                currentConfig.Height = themeConfig.Height;
                currentConfig.CornerRadius = themeConfig.CornerRadius;
                currentConfig.ShowCloseButton = themeConfig.ShowCloseButton;
                currentConfig.ShowTimestamp = themeConfig.ShowTimestamp;
                currentConfig.AutoCloseDelay = themeConfig.AutoCloseDelay;

                await _settingsManager.UpdateNotificationConfigurationAsync(currentConfig);
                UpdateUIFromSettings();

                ShowSuccessMessage("Theme Applied", "Theme has been applied successfully");
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Theme Error", $"Failed to apply theme: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets ComboBox selection by content
        /// </summary>
        private void SetComboBoxSelection(System.Windows.Controls.ComboBox comboBox, string value)
        {
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is System.Windows.Controls.ComboBoxItem item && item.Content.ToString() == value)
                {
                    comboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        /// Shows a success message
        /// </summary>
        private void ShowSuccessMessage(string title, string message)
        {
            var notification = NotificationData.CreateSuccess(title, message);
            _notificationManager.ShowNotification(notification);
        }

        /// <summary>
        /// Shows an error message
        /// </summary>
        private void ShowErrorMessage(string title, string message)
        {
            var notification = NotificationData.CreateError(title, message);
            _notificationManager.ShowNotification(notification);
        }

        /// <summary>
        /// Shows a not implemented message
        /// </summary>
        private void ShowNotImplementedMessage(string feature)
        {
            var notification = NotificationData.CreateInfo(
                "Feature Not Implemented",
                $"{feature} feature will be available in a future update"
            );
            _notificationManager.ShowNotification(notification);
        }

        /// <summary>
        /// Shows a confirmation dialog
        /// </summary>
        private bool ShowConfirmationDialog(string title, string message)
        {
            var result = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// Display item for category data grid
    /// </summary>
    public class CategoryDisplayItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public string DefaultTimeout { get; set; } = string.Empty;
        public bool AllowAutoDismiss { get; set; }
        public string IconContent { get; set; } = string.Empty;
    }

    #endregion
}