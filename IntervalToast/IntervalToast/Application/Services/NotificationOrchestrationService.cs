using IntervalToast.Application.Interfaces;
using IntervalToast.Application.Models;
using IntervalToast.Domain.Entities;
using IntervalToast.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Timer = System.Threading.Timer;

namespace IntervalToast.Application.Services;

/// <summary>
/// Orchestrates notification display, scheduling, and lifecycle management
/// </summary>
public sealed class NotificationOrchestrationService : IDisposable
{
    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly IAnimationService _animationService;
    private readonly IWindowPositionService _windowPositionService;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationOrchestrationService> _logger;
    private readonly Timer _schedulingTimer;
    private readonly SemaphoreSlim _displaySemaphore;
    private bool _disposed;

    public NotificationOrchestrationService(
        INotificationService notificationService,
        ISettingsService settingsService,
        IAnimationService animationService,
        IWindowPositionService windowPositionService,
        INotificationRepository notificationRepository,
        ILogger<NotificationOrchestrationService> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
        _windowPositionService = windowPositionService ?? throw new ArgumentNullException(nameof(windowPositionService));
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _displaySemaphore = new SemaphoreSlim(1, 1);
        _schedulingTimer = new Timer(ProcessScheduledNotifications, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        _logger.LogInformation("Notification orchestration service initialized");
    }

    /// <summary>
    /// Processes a notification request
    /// </summary>
    public async Task<Notification> ProcessNotificationRequestAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.IsValid)
            throw new ArgumentException("Invalid notification request", nameof(request));

        _logger.LogDebug("Processing notification request: {Title}", request.Title);

        try
        {
            // Create notification entity
            var notification = new Notification(
                request.Title,
                request.Message,
                request.Category,
                request.Priority,
                request.Style,
                request.ScheduledAt,
                request.DisplayDurationMs,
                request.CanDismiss);

            // Store in repository
            await _notificationRepository.AddAsync(notification, cancellationToken);

            // If immediate display, show now
            if (request.ScheduledAt == null || request.ScheduledAt <= DateTimeOffset.UtcNow)
            {
                await DisplayNotificationAsync(notification, cancellationToken);
            }

            _logger.LogInformation("Notification processed successfully: {NotificationId}", notification.Id);
            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process notification request: {Title}", request.Title);
            throw;
        }
    }

    /// <summary>
    /// Displays a notification immediately
    /// </summary>
    private async Task DisplayNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _displaySemaphore.WaitAsync(cancellationToken);
        try
        {
            // Check if we've reached the maximum concurrent notifications
            var settings = await _settingsService.GetSettingsAsync(cancellationToken);
            var activeNotifications = await _notificationService.GetActiveNotificationsAsync(cancellationToken);

            if (activeNotifications.Count() >= settings.DisplaySettings.MaxConcurrentNotifications)
            {
                _logger.LogWarning("Maximum concurrent notifications reached, queueing notification {NotificationId}", notification.Id);
                return; // Will be processed by the timer later
            }

            // Show the notification
            await _notificationService.ShowNotificationAsync(
                notification.Title,
                notification.Message,
                notification.Category,
                notification.Priority,
                notification.Style,
                cancellationToken);

            notification.MarkAsDisplayed();
            await _notificationRepository.UpdateAsync(notification, cancellationToken);

            _logger.LogDebug("Notification displayed: {NotificationId}", notification.Id);
        }
        finally
        {
            _displaySemaphore.Release();
        }
    }

    /// <summary>
    /// Timer callback to process scheduled notifications
    /// </summary>
    private async void ProcessScheduledNotifications(object? state)
    {
        try
        {
            var scheduledNotifications = await _notificationRepository.GetScheduledForNowAsync();

            foreach (var notification in scheduledNotifications)
            {
                try
                {
                    await DisplayNotificationAsync(notification);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to display scheduled notification {NotificationId}", notification.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled notifications");
        }
    }

    /// <summary>
    /// Performs cleanup of old notifications
    /// </summary>
    public async Task PerformMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting notification maintenance");

            // Clean up old dismissed notifications (older than 7 days)
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-7);
            var cleanedCount = await _notificationRepository.CleanupOldNotificationsAsync(cutoffDate, cancellationToken);

            if (cleanedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old notifications", cleanedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during notification maintenance");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _schedulingTimer?.Dispose();
        _displaySemaphore?.Dispose();
        _disposed = true;

        _logger.LogInformation("Notification orchestration service disposed");
    }
}