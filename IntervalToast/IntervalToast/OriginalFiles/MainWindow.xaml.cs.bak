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
        private readonly ScheduleManager _scheduleManager;
        private bool _isMinimizedToTray = false;
        private bool _suppressCloseToTray = false;
        private List<HotkeyDisplayItem> _hotkeyDisplayItems = new();

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
            _scheduleManager = new ScheduleManager(_notificationManager, _settingsManager);

            // Initialize system tray manager
            try
            {
                _systemTrayManager = new SystemTrayManager(_notificationManager, _hotkeyManager, _settingsManager, _scheduleManager);
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

            // Initialize schedule UI
            InitializeScheduleUI();
            UpdateScheduleUI();

            // Initialize hotkey UI
            InitializeHotkeyUI();
            UpdateHotkeyUI();

            // Start schedule manager
            _scheduleManager.Start();

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

        // Schedule tab buttons
        private void ToggleSchedulingBtn_Click(object sender, RoutedEventArgs e)
        {
            var settings = _settingsManager.CurrentSettings.ScheduleSettings;
            settings.SchedulingEnabled = !settings.SchedulingEnabled;

            if (settings.SchedulingEnabled)
            {
                _scheduleManager.Start();
                ToggleSchedulingBtn.Content = "Disable Scheduling";
                ToggleSchedulingBtn.Style = (Style)FindResource("DangerButtonStyle");
            }
            else
            {
                _scheduleManager.Stop();
                ToggleSchedulingBtn.Content = "Enable Scheduling";
                ToggleSchedulingBtn.Style = (Style)FindResource("SuccessButtonStyle");
            }

            _settingsManager.SaveSettingsAsync();
            UpdateScheduleUI();
        }

        private void RefreshSchedulesBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateScheduleUI();
        }

        private void CreateScheduleBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var schedule = CreateScheduleFromForm();
                _scheduleManager.AddSchedule(schedule);
                UpdateScheduleUI();
                ClearScheduleForm();

                ShowSuccessMessage("Schedule Created", $"Schedule '{schedule.Name}' has been created successfully.");
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error Creating Schedule", ex.Message);
            }
        }

        private void TestScheduleBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var schedule = CreateScheduleFromForm();

                // Create notification from template
                var notification = new NotificationData
                {
                    Title = schedule.NotificationTemplate.Title,
                    Message = schedule.NotificationTemplate.Message,
                    Category = schedule.NotificationTemplate.Category,
                    Priority = schedule.NotificationTemplate.Priority,
                    IconContent = schedule.NotificationTemplate.IconContent
                };

                // Add test metadata
                notification.Metadata["IsTest"] = true;
                notification.Metadata["TestTime"] = DateTime.Now;

                _notificationManager.ShowNotification(notification);
                ShowSuccessMessage("Test Notification", "Test notification sent successfully.");
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error Testing Schedule", ex.Message);
            }
        }

        private void ClearFormBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearScheduleForm();
        }

        private void DeleteSelectedBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ScheduleDataGrid.SelectedItem is NotificationSchedule schedule)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete the schedule '{schedule.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _scheduleManager.RemoveSchedule(schedule.Id);
                    UpdateScheduleUI();
                    ShowSuccessMessage("Schedule Deleted", $"Schedule '{schedule.Name}' has been deleted.");
                }
            }
            else
            {
                ShowErrorMessage("No Selection", "Please select a schedule to delete.");
            }
        }

        private void ClearAllSchedulesBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to delete ALL schedules? This cannot be undone.",
                "Confirm Clear All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _scheduleManager.ClearAllSchedules();
                UpdateScheduleUI();
                ShowSuccessMessage("Schedules Cleared", "All schedules have been deleted.");
            }
        }

        private void EditScheduleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is NotificationSchedule schedule)
            {
                LoadScheduleIntoForm(schedule);
                ShowInfoMessage("Schedule Loaded", $"Schedule '{schedule.Name}' has been loaded into the form for editing.");
            }
        }

        private void TestExistingScheduleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is NotificationSchedule schedule)
            {
                _scheduleManager.TriggerSchedule(schedule.Id);
                ShowSuccessMessage("Schedule Triggered", $"Schedule '{schedule.Name}' has been triggered manually.");
            }
        }

        private void CloneScheduleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is NotificationSchedule schedule)
            {
                var clonedSchedule = schedule.Clone();
                _scheduleManager.AddSchedule(clonedSchedule);
                UpdateScheduleUI();
                ShowSuccessMessage("Schedule Cloned", $"Schedule '{schedule.Name}' has been cloned as '{clonedSchedule.Name}'.");
            }
        }

        private void DeleteScheduleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is NotificationSchedule schedule)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete the schedule '{schedule.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _scheduleManager.RemoveSchedule(schedule.Id);
                    UpdateScheduleUI();
                    ShowSuccessMessage("Schedule Deleted", $"Schedule '{schedule.Name}' has been deleted.");
                }
            }
        }

        private void RecurrenceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateIntervalUI();
        }

        // Hotkey tab buttons
        private void ToggleHotkeySystemBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _hotkeyManager.IsEnabled = !_hotkeyManager.IsEnabled;
                UpdateHotkeyUI();

                var statusMessage = _hotkeyManager.IsEnabled ? "Hotkey system enabled" : "Hotkey system disabled";
                ShowSuccessMessage("Hotkey System", statusMessage);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Hotkey System Error", $"Failed to toggle hotkey system: {ex.Message}");
            }
        }

        private void RefreshHotkeysBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateHotkeyUI();
            ShowInfoMessage("Hotkeys Refreshed", "Hotkey list has been refreshed");
        }

        private void TestAllHotkeysBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var registeredHotkeys = _hotkeyManager.GetRegisteredHotkeys();
                var enabledCount = registeredHotkeys.Count(h => h.IsEnabled);

                ShowInfoMessage("Hotkey Test", $"Testing {enabledCount} enabled hotkeys. Try pressing them to verify functionality.");
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Hotkey Test Error", ex.Message);
            }
        }

        private void AddHotkeyBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newHotkey = new HotkeyConfiguration
                {
                    Description = "New Custom Hotkey",
                    IsEnabled = true,
                    Action = () => ShowInfoMessage("Custom Hotkey", "Custom hotkey action executed")
                };

                var editWindow = new HotkeyEditWindow(newHotkey);
                editWindow.Owner = this;

                if (editWindow.ShowDialog() == true && editWindow.EditedHotkey != null)
                {
                    var success = _hotkeyManager.RegisterHotkey(editWindow.EditedHotkey);
                    if (success)
                    {
                        UpdateHotkeyUI();
                        ShowSuccessMessage("Hotkey Added", $"Hotkey '{editWindow.EditedHotkey.DisplayName}' has been added successfully");
                    }
                    else
                    {
                        ShowErrorMessage("Hotkey Registration Failed", "Failed to register the new hotkey. It may conflict with an existing hotkey.");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Add Hotkey Error", ex.Message);
            }
        }

        private void ResetHotkeysBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ShowConfirmationDialog("Reset Hotkeys", "Are you sure you want to reset all hotkeys to defaults? This will remove any custom hotkeys you've added."))
            {
                try
                {
                    _hotkeyManager.UnregisterAllHotkeys();
                    _hotkeyManager.RegisterDefaultHotkeys();
                    UpdateHotkeyUI();
                    ShowSuccessMessage("Hotkeys Reset", "All hotkeys have been reset to default configuration");
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Reset Hotkeys Error", ex.Message);
                }
            }
        }

        private void EditHotkeyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is HotkeyDisplayItem displayItem)
            {
                try
                {
                    var hotkey = displayItem.HotkeyConfiguration;
                    var editWindow = new HotkeyEditWindow(hotkey);
                    editWindow.Owner = this;

                    if (editWindow.ShowDialog() == true && editWindow.EditedHotkey != null)
                    {
                        // Unregister old hotkey
                        _hotkeyManager.UnregisterHotkey(hotkey);

                        // Register new hotkey
                        var success = _hotkeyManager.RegisterHotkey(editWindow.EditedHotkey);
                        if (success)
                        {
                            UpdateHotkeyUI();
                            ShowSuccessMessage("Hotkey Updated", $"Hotkey has been updated to '{editWindow.EditedHotkey.DisplayName}'");
                        }
                        else
                        {
                            // Re-register original if new one failed
                            _hotkeyManager.RegisterHotkey(hotkey);
                            ShowErrorMessage("Hotkey Update Failed", "Failed to update hotkey. Original hotkey restored.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Edit Hotkey Error", ex.Message);
                }
            }
        }

        private void TestHotkeyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is HotkeyDisplayItem displayItem)
            {
                try
                {
                    var hotkey = displayItem.HotkeyConfiguration;
                    if (hotkey.Action != null)
                    {
                        hotkey.Action.Invoke();
                        ShowSuccessMessage("Hotkey Test", $"Hotkey '{hotkey.DisplayName}' action executed successfully");
                    }
                    else
                    {
                        ShowInfoMessage("Hotkey Test", $"Hotkey '{hotkey.DisplayName}' has no action defined");
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Hotkey Test Error", ex.Message);
                }
            }
        }

        private void CloneHotkeyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is HotkeyDisplayItem displayItem)
            {
                try
                {
                    var originalHotkey = displayItem.HotkeyConfiguration;
                    var clonedHotkey = originalHotkey.Clone();
                    clonedHotkey.Description = $"{originalHotkey.Description} (Copy)";
                    clonedHotkey.Id = 0; // Reset ID to get a new one

                    var editWindow = new HotkeyEditWindow(clonedHotkey);
                    editWindow.Owner = this;

                    if (editWindow.ShowDialog() == true && editWindow.EditedHotkey != null)
                    {
                        var success = _hotkeyManager.RegisterHotkey(editWindow.EditedHotkey);
                        if (success)
                        {
                            UpdateHotkeyUI();
                            ShowSuccessMessage("Hotkey Cloned", $"Hotkey has been cloned as '{editWindow.EditedHotkey.DisplayName}'");
                        }
                        else
                        {
                            ShowErrorMessage("Hotkey Clone Failed", "Failed to register the cloned hotkey");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Clone Hotkey Error", ex.Message);
                }
            }
        }

        private void DeleteHotkeyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is HotkeyDisplayItem displayItem)
            {
                var hotkey = displayItem.HotkeyConfiguration;
                if (ShowConfirmationDialog("Delete Hotkey", $"Are you sure you want to delete the hotkey '{hotkey.DisplayName}'?"))
                {
                    try
                    {
                        var success = _hotkeyManager.UnregisterHotkey(hotkey);
                        if (success)
                        {
                            UpdateHotkeyUI();
                            ShowSuccessMessage("Hotkey Deleted", $"Hotkey '{hotkey.DisplayName}' has been deleted");
                        }
                        else
                        {
                            ShowErrorMessage("Delete Failed", "Failed to delete the hotkey");
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage("Delete Hotkey Error", ex.Message);
                    }
                }
            }
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
        /// Shows an info message
        /// </summary>
        private void ShowInfoMessage(string title, string message)
        {
            var notification = NotificationData.CreateInfo(title, message);
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

        /// <summary>
        /// Initializes the schedule UI components
        /// </summary>
        private void InitializeScheduleUI()
        {
            // Populate hour dropdown (0-23)
            for (int i = 0; i < 24; i++)
            {
                StartHourComboBox.Items.Add(i.ToString("00"));
            }
            StartHourComboBox.SelectedItem = DateTime.Now.Hour.ToString("00");

            // Populate minute dropdown (00, 15, 30, 45)
            for (int i = 0; i < 60; i += 15)
            {
                StartMinuteComboBox.Items.Add(i.ToString("00"));
            }
            StartMinuteComboBox.SelectedItem = "00";

            // Initialize form
            ClearScheduleForm();
            UpdateIntervalUI();

            // Subscribe to schedule manager events
            _scheduleManager.ScheduleAdded += OnScheduleAdded;
            _scheduleManager.ScheduleRemoved += OnScheduleRemoved;
            _scheduleManager.ScheduleUpdated += OnScheduleUpdated;
            _scheduleManager.ScheduleTriggered += OnScheduleTriggered;
            _scheduleManager.StatusChanged += OnScheduleManagerStatusChanged;
        }

        /// <summary>
        /// Updates the schedule UI with current data
        /// </summary>
        private void UpdateScheduleUI()
        {
            try
            {
                // Update status display
                var settings = _settingsManager.CurrentSettings.ScheduleSettings;
                var isEnabled = settings.SchedulingEnabled && _scheduleManager.IsRunning;

                SchedulingStatusText.Text = isEnabled ? "Enabled" : "Disabled";
                SchedulingStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    isEnabled ? System.Windows.Media.Color.FromRgb(39, 174, 96) : System.Windows.Media.Color.FromRgb(231, 76, 60));

                // Update button
                ToggleSchedulingBtn.Content = isEnabled ? "Disable Scheduling" : "Enable Scheduling";
                ToggleSchedulingBtn.Style = (Style)FindResource(isEnabled ? "DangerButtonStyle" : "SuccessButtonStyle");

                // Update schedule counts
                var stats = _scheduleManager.GetStatistics();
                ActiveSchedulesText.Text = $"{stats.EnabledSchedules} Active";

                // Update next schedule time
                var nextTime = _scheduleManager.NextScheduledTime;
                NextScheduleText.Text = nextTime?.ToString("MM/dd HH:mm") ?? "None";

                // Update data grid
                ScheduleDataGrid.ItemsSource = _scheduleManager.Schedules.ToList();

                // Update statistics
                TotalSchedulesText.Text = stats.TotalSchedules.ToString();
                EnabledSchedulesText.Text = stats.EnabledSchedules.ToString();
                RecurringSchedulesText.Text = stats.RecurringSchedules.ToString();
                TotalTriggersText.Text = stats.TotalTriggers.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating schedule UI: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the interval UI based on selected recurrence
        /// </summary>
        private void UpdateIntervalUI()
        {
            // Check if UI controls are initialized
            if (RecurrenceComboBox == null || IntervalLabel == null || IntervalPanel == null || IntervalUnitLabel == null)
                return;

            var selectedIndex = RecurrenceComboBox.SelectedIndex;
            var isRecurring = selectedIndex > 0; // 0 = One Time

            IntervalLabel.Visibility = isRecurring ? Visibility.Visible : Visibility.Hidden;
            IntervalPanel.Visibility = isRecurring ? Visibility.Visible : Visibility.Hidden;

            if (isRecurring)
            {
                string unitText = selectedIndex switch
                {
                    1 => "minute(s)",
                    2 => "hour(s)",
                    3 => "day(s)",
                    4 => "week(s)",
                    5 => "month(s)",
                    _ => "time(s)"
                };
                IntervalUnitLabel.Text = unitText;
            }
        }

        /// <summary>
        /// Creates a schedule from the form data
        /// </summary>
        private NotificationSchedule CreateScheduleFromForm()
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(ScheduleNameTextBox.Text))
                throw new ArgumentException("Schedule name is required.");

            if (string.IsNullOrWhiteSpace(NotificationTitleTextBox.Text))
                throw new ArgumentException("Notification title is required.");

            if (string.IsNullOrWhiteSpace(NotificationMessageTextBox.Text))
                throw new ArgumentException("Notification message is required.");

            // Parse time
            var date = StartDatePicker.SelectedDate ?? DateTime.Today;
            var hour = int.Parse(StartHourComboBox.SelectedItem?.ToString() ?? "0");
            var minute = int.Parse(StartMinuteComboBox.SelectedItem?.ToString() ?? "0");
            var startTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);

            // Parse recurrence
            var recurrenceIndex = RecurrenceComboBox.SelectedIndex;
            var recurrence = recurrenceIndex switch
            {
                0 => ScheduleRecurrence.OneTime,
                1 => ScheduleRecurrence.Minutes,
                2 => ScheduleRecurrence.Hours,
                3 => ScheduleRecurrence.Days,
                4 => ScheduleRecurrence.Weekly,
                5 => ScheduleRecurrence.Monthly,
                _ => ScheduleRecurrence.OneTime
            };

            // Parse interval value
            var intervalValue = 1;
            if (recurrence != ScheduleRecurrence.OneTime)
            {
                if (!int.TryParse(IntervalValueTextBox.Text, out intervalValue) || intervalValue <= 0)
                    throw new ArgumentException("Interval value must be a positive number.");
            }

            // Parse category and priority
            var category = NotificationCategoryComboBox.SelectedIndex switch
            {
                0 => NotificationCategory.Info,
                1 => NotificationCategory.Success,
                2 => NotificationCategory.Warning,
                3 => NotificationCategory.Error,
                4 => NotificationCategory.System,
                _ => NotificationCategory.Info
            };

            var priority = NotificationPriorityComboBox.SelectedIndex switch
            {
                0 => NotificationPriority.Low,
                1 => NotificationPriority.Normal,
                2 => NotificationPriority.High,
                3 => NotificationPriority.Critical,
                _ => NotificationPriority.Normal
            };

            // Create schedule
            var schedule = new NotificationSchedule
            {
                Name = ScheduleNameTextBox.Text.Trim(),
                StartTime = startTime,
                Recurrence = recurrence,
                IntervalValue = intervalValue,
                NotificationTemplate = new NotificationData
                {
                    Title = NotificationTitleTextBox.Text.Trim(),
                    Message = NotificationMessageTextBox.Text.Trim(),
                    Category = category,
                    Priority = priority
                }
            };

            return schedule;
        }

        /// <summary>
        /// Clears the schedule creation form
        /// </summary>
        private void ClearScheduleForm()
        {
            ScheduleNameTextBox.Text = "My Reminder";
            NotificationTitleTextBox.Text = "Reminder";
            NotificationMessageTextBox.Text = "Time for a break!";
            NotificationCategoryComboBox.SelectedIndex = 0;
            NotificationPriorityComboBox.SelectedIndex = 1;
            StartDatePicker.SelectedDate = DateTime.Today;
            StartHourComboBox.SelectedItem = DateTime.Now.Hour.ToString("00");
            StartMinuteComboBox.SelectedItem = "00";
            RecurrenceComboBox.SelectedIndex = 0;
            IntervalValueTextBox.Text = "1";
            UpdateIntervalUI();
        }

        /// <summary>
        /// Loads a schedule into the form for editing
        /// </summary>
        private void LoadScheduleIntoForm(NotificationSchedule schedule)
        {
            ScheduleNameTextBox.Text = schedule.Name;
            NotificationTitleTextBox.Text = schedule.NotificationTemplate.Title;
            NotificationMessageTextBox.Text = schedule.NotificationTemplate.Message;

            // Set category
            NotificationCategoryComboBox.SelectedIndex = schedule.NotificationTemplate.Category switch
            {
                NotificationCategory.Info => 0,
                NotificationCategory.Success => 1,
                NotificationCategory.Warning => 2,
                NotificationCategory.Error => 3,
                NotificationCategory.System => 4,
                _ => 0
            };

            // Set priority
            NotificationPriorityComboBox.SelectedIndex = schedule.NotificationTemplate.Priority switch
            {
                NotificationPriority.Low => 0,
                NotificationPriority.Normal => 1,
                NotificationPriority.High => 2,
                NotificationPriority.Critical => 3,
                _ => 1
            };

            // Set time
            StartDatePicker.SelectedDate = schedule.StartTime.Date;
            StartHourComboBox.SelectedItem = schedule.StartTime.Hour.ToString("00");
            StartMinuteComboBox.SelectedItem = schedule.StartTime.Minute.ToString("00");

            // Set recurrence
            RecurrenceComboBox.SelectedIndex = schedule.Recurrence switch
            {
                ScheduleRecurrence.OneTime => 0,
                ScheduleRecurrence.Minutes => 1,
                ScheduleRecurrence.Hours => 2,
                ScheduleRecurrence.Days => 3,
                ScheduleRecurrence.Weekly => 4,
                ScheduleRecurrence.Monthly => 5,
                _ => 0
            };

            IntervalValueTextBox.Text = schedule.IntervalValue.ToString();
            UpdateIntervalUI();
        }

        /// <summary>
        /// Event handler for schedule added
        /// </summary>
        private void OnScheduleAdded(object? sender, ScheduleEventArgs e)
        {
            Dispatcher.BeginInvoke(() => UpdateScheduleUI());
        }

        /// <summary>
        /// Event handler for schedule removed
        /// </summary>
        private void OnScheduleRemoved(object? sender, ScheduleEventArgs e)
        {
            Dispatcher.BeginInvoke(() => UpdateScheduleUI());
        }

        /// <summary>
        /// Event handler for schedule updated
        /// </summary>
        private void OnScheduleUpdated(object? sender, ScheduleEventArgs e)
        {
            Dispatcher.BeginInvoke(() => UpdateScheduleUI());
        }

        /// <summary>
        /// Event handler for schedule triggered
        /// </summary>
        private void OnScheduleTriggered(object? sender, ScheduleTriggeredEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdateScheduleUI();
                Debug.WriteLine($"Schedule '{e.Schedule.Name}' triggered - Count: {e.Schedule.TriggerCount}");
            });
        }

        /// <summary>
        /// Event handler for schedule manager status change
        /// </summary>
        private void OnScheduleManagerStatusChanged(object? sender, ScheduleManagerStatusEventArgs e)
        {
            Dispatcher.BeginInvoke(() => UpdateScheduleUI());
        }

        /// <summary>
        /// Initializes the hotkey UI components
        /// </summary>
        private void InitializeHotkeyUI()
        {
            try
            {
                // Subscribe to hotkey manager events
                _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
                _hotkeyManager.HotkeyRegistered += OnHotkeyRegistered;
                _hotkeyManager.HotkeyUnregistered += OnHotkeyUnregistered;
                _hotkeyManager.HotkeyRegistrationFailed += OnHotkeyRegistrationFailed;
                _hotkeyManager.SystemStateChanged += OnHotkeySystemStateChanged;

                Debug.WriteLine("Hotkey UI initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize hotkey UI: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the hotkey UI with current data
        /// </summary>
        private void UpdateHotkeyUI()
        {
            try
            {
                // Update system status
                var isEnabled = _hotkeyManager.IsEnabled;
                HotkeySystemStatusText.Text = isEnabled ? "Enabled" : "Disabled";
                HotkeySystemStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    isEnabled ? System.Windows.Media.Color.FromRgb(39, 174, 96) : System.Windows.Media.Color.FromRgb(231, 76, 60));

                // Update toggle button
                ToggleHotkeySystemBtn.Content = isEnabled ? "Disable Hotkeys" : "Enable Hotkeys";
                ToggleHotkeySystemBtn.Style = (Style)FindResource(isEnabled ? "DangerButtonStyle" : "SuccessButtonStyle");

                // Get registered hotkeys
                var registeredHotkeys = _hotkeyManager.GetRegisteredHotkeys();
                RegisteredHotkeysText.Text = $"{registeredHotkeys.Count} Registered";

                // Check for conflicts (simplified - just show 0 for now)
                HotkeyConflictsText.Text = "0 Conflicts";

                // Create display items for DataGrid
                _hotkeyDisplayItems.Clear();
                foreach (var hotkey in registeredHotkeys)
                {
                    _hotkeyDisplayItems.Add(new HotkeyDisplayItem
                    {
                        HotkeyConfiguration = hotkey,
                        IsEnabled = hotkey.IsEnabled,
                        DisplayName = hotkey.DisplayName,
                        Description = hotkey.Description,
                        StatusText = hotkey.IsRegistered ? "Active" : "Inactive"
                    });
                }

                // Update DataGrid
                HotkeyDataGrid.ItemsSource = null;
                HotkeyDataGrid.ItemsSource = _hotkeyDisplayItems;

                Debug.WriteLine($"Hotkey UI updated with {registeredHotkeys.Count} hotkeys");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating hotkey UI: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for hotkey pressed
        /// </summary>
        private void OnHotkeyPressed(object? sender, HotkeyEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Debug.WriteLine($"Hotkey pressed: {e.Hotkey.DisplayName}");
                UpdateRecentActivity($"Hotkey: {e.Hotkey.Description}");
            });
        }

        /// <summary>
        /// Event handler for hotkey registered
        /// </summary>
        private void OnHotkeyRegistered(object? sender, HotkeyEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdateHotkeyUI();
                Debug.WriteLine($"Hotkey registered: {e.Hotkey.DisplayName}");
            });
        }

        /// <summary>
        /// Event handler for hotkey unregistered
        /// </summary>
        private void OnHotkeyUnregistered(object? sender, HotkeyEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdateHotkeyUI();
                Debug.WriteLine($"Hotkey unregistered: {e.Hotkey.DisplayName}");
            });
        }

        /// <summary>
        /// Event handler for hotkey registration failure
        /// </summary>
        private void OnHotkeyRegistrationFailed(object? sender, HotkeyRegistrationFailedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdateHotkeyUI();
                ShowErrorMessage("Hotkey Registration Failed",
                    $"Failed to register hotkey '{e.Hotkey.DisplayName}': {e.Exception.Message}");
            });
        }

        /// <summary>
        /// Event handler for hotkey system state change
        /// </summary>
        private void OnHotkeySystemStateChanged(object? sender, HotkeySystemStateChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdateHotkeyUI();
                Debug.WriteLine($"Hotkey system state changed: {(e.IsEnabled ? "Enabled" : "Disabled")}");
            });
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

    /// <summary>
    /// Display item for hotkey data grid
    /// </summary>
    public class HotkeyDisplayItem
    {
        public HotkeyConfiguration HotkeyConfiguration { get; set; } = new();
        public bool IsEnabled { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
    }

    #endregion
}