using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IntervalToast.Application.Interfaces;
using IntervalToast.Application.Models;
using IntervalToast.Domain.Entities;
using IntervalToast.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace IntervalToast.Presentation.ViewModels;

/// <summary>
/// View model for the main configuration window
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly IHotkeyService _hotkeyService;
    private readonly ILogger<MainWindowViewModel> _logger;

    [ObservableProperty]
    private bool _isSystemEnabled = true;

    [ObservableProperty]
    private int _activeNotificationCount;

    [ObservableProperty]
    private int _pendingNotificationCount;

    [ObservableProperty]
    private string _statusMessage = "System Ready";

    [ObservableProperty]
    private ApplicationSettings? _currentSettings;

    [ObservableProperty]
    private ObservableCollection<NotificationDisplayItem> _recentNotifications = new();

    [ObservableProperty]
    private ObservableCollection<HotkeyDisplayItem> _hotkeyBindings = new();

    [ObservableProperty]
    private bool _isLoading;

    public MainWindowViewModel(
        INotificationService notificationService,
        ISettingsService settingsService,
        // IHotkeyService hotkeyService, // Commented out until implementation is ready
        ILogger<MainWindowViewModel> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        // _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to events - temporarily commented out until events are implemented
        // _notificationService.NotificationDisplayed += OnNotificationDisplayed;
        // _notificationService.NotificationDismissed += OnNotificationDismissed;
        // _settingsService.SettingsUpdated += OnSettingsUpdated;
        // _hotkeyService.HotkeyPressed += OnHotkeyPressed; // Commented out until implementation is ready

        // Initialize
        _ = InitializeAsync();
    }

    /// <summary>
    /// Initializes the view model
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading...";

            // Load settings
            CurrentSettings = await _settingsService.GetSettingsAsync();

            // Load notification status
            await RefreshNotificationStatusAsync();

            // Load hotkey bindings
            // await RefreshHotkeyBindingsAsync(); // Commented out until implementation is ready

            StatusMessage = "System Ready";
            _logger.LogInformation("Main window view model initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize main window view model");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refreshes the notification status
    /// </summary>
    [RelayCommand]
    private async Task RefreshNotificationStatusAsync()
    {
        try
        {
            var status = await _notificationService.GetQueueStatusAsync();
            ActiveNotificationCount = status.ActiveCount;
            PendingNotificationCount = status.PendingCount;
            IsSystemEnabled = status.IsSystemEnabled;

            // Load recent notifications
            var recentNotifications = await _notificationService.GetNotificationHistoryAsync(10);
            RecentNotifications.Clear();
            foreach (var notification in recentNotifications)
            {
                RecentNotifications.Add(new NotificationDisplayItem(notification));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh notification status");
            StatusMessage = $"Error refreshing status: {ex.Message}";
        }
    }

    /// <summary>
    /// Refreshes the hotkey bindings
    /// </summary>
    [RelayCommand]
    private async Task RefreshHotkeyBindingsAsync()
    {
        try
        {
            // TODO: Implement when hotkey service is ready
            // var bindings = await _hotkeyService.GetRegisteredHotkeysAsync();
            // HotkeyBindings.Clear();
            // foreach (var binding in bindings)
            // {
            //     HotkeyBindings.Add(new HotkeyDisplayItem(binding));
            // }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh hotkey bindings");
        }
    }

    /// <summary>
    /// Shows a test notification
    /// </summary>
    [RelayCommand]
    private async Task ShowTestNotificationAsync()
    {
        try
        {
            await _notificationService.ShowNotificationAsync(
                "Test Notification",
                "This is a test notification from IntervalToast",
                NotificationCategory.Info);

            StatusMessage = "Test notification sent";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show test notification");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Dismisses all active notifications
    /// </summary>
    [RelayCommand]
    private async Task DismissAllNotificationsAsync()
    {
        try
        {
            await _notificationService.DismissAllAsync();
            await RefreshNotificationStatusAsync();
            StatusMessage = "All notifications dismissed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dismiss all notifications");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Opens the settings dialog
    /// </summary>
    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        try
        {
            // TODO: Implement settings dialog
            StatusMessage = "Settings dialog not yet implemented";
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open settings");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Event handler for notification displayed
    /// </summary>
    private void OnNotificationDisplayed(object? sender, NotificationDisplayedEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(async () =>
        {
            await RefreshNotificationStatusAsync();
        });
    }

    /// <summary>
    /// Event handler for notification dismissed
    /// </summary>
    private void OnNotificationDismissed(object? sender, NotificationDismissedEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(async () =>
        {
            await RefreshNotificationStatusAsync();
        });
    }

    /// <summary>
    /// Event handler for settings updated
    /// </summary>
    private void OnSettingsUpdated(object? sender, SettingsUpdatedEventArgs e)
    {
        CurrentSettings = e.Settings;
        StatusMessage = "Settings updated";
    }

    /// <summary>
    /// Event handler for hotkey pressed
    /// </summary>
    private void OnHotkeyPressed(object? sender, object e) // Changed parameter until implementation is ready
    {
        _logger.LogInformation("Hotkey pressed");
        StatusMessage = "Hotkey pressed";
    }
}

/// <summary>
/// Display item for notifications in the UI
/// </summary>
public sealed class NotificationDisplayItem
{
    public Guid Id { get; }
    public string Title { get; }
    public string Message { get; }
    public NotificationCategory Category { get; }
    public NotificationPriority Priority { get; }
    public DateTimeOffset CreatedAt { get; }
    public bool IsDisplayed { get; }
    public bool IsDismissed { get; }

    public NotificationDisplayItem(Notification notification)
    {
        Id = notification.Id;
        Title = notification.Title;
        Message = notification.Message;
        Category = notification.Category;
        Priority = notification.Priority;
        CreatedAt = notification.CreatedAt;
        IsDisplayed = notification.IsDisplayed;
        IsDismissed = notification.IsDismissed;
    }
}

/// <summary>
/// Display item for hotkey bindings in the UI
/// </summary>
public sealed class HotkeyDisplayItem
{
    public Guid Id { get; }
    public string Name { get; }
    public string HotkeyText { get; }
    public string Action { get; }
    public bool IsEnabled { get; }
    public bool IsRegistered { get; }
    public int UsageCount { get; }

    public HotkeyDisplayItem(HotkeyBinding binding)
    {
        Id = binding.Id;
        Name = binding.Name;
        HotkeyText = binding.HotkeyDefinition.ToString();
        Action = binding.Action;
        IsEnabled = binding.IsEnabled;
        IsRegistered = binding.IsRegistered;
        UsageCount = binding.UsageCount;
    }
}