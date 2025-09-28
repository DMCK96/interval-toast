using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using IntervalToast.Domain.Entities;
using IntervalToast.Infrastructure.Persistence;

namespace IntervalToast.Infrastructure.Services
{
    /// <summary>
    /// Manages scheduled notifications and background timer execution
    /// </summary>
    public class ScheduleManager : IDisposable
    {
        #region Fields

        private readonly NotificationManager _notificationManager;
        private readonly SettingsManager _settingsManager;
        private readonly System.Timers.Timer _mainTimer;
        private readonly object _scheduleLock = new object();
        private readonly List<NotificationSchedule> _activeSchedules = new();
        private bool _isRunning = false;
        private bool _disposed = false;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a schedule is triggered
        /// </summary>
        public event EventHandler<ScheduleTriggeredEventArgs>? ScheduleTriggered;

        /// <summary>
        /// Fired when a schedule is added
        /// </summary>
        public event EventHandler<ScheduleEventArgs>? ScheduleAdded;

        /// <summary>
        /// Fired when a schedule is removed
        /// </summary>
        public event EventHandler<ScheduleEventArgs>? ScheduleRemoved;

        /// <summary>
        /// Fired when a schedule is updated
        /// </summary>
        public event EventHandler<ScheduleEventArgs>? ScheduleUpdated;

        /// <summary>
        /// Fired when the schedule manager starts or stops
        /// </summary>
        public event EventHandler<ScheduleManagerStatusEventArgs>? StatusChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ScheduleManager
        /// </summary>
        public ScheduleManager(NotificationManager notificationManager, SettingsManager settingsManager)
        {
            _notificationManager = notificationManager ?? throw new ArgumentNullException(nameof(notificationManager));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));

            // Initialize the main timer - check every 30 seconds for pending schedules
            _mainTimer = new System.Timers.Timer(30000); // 30 seconds
            _mainTimer.Elapsed += OnTimerElapsed;
            _mainTimer.AutoReset = true;

            // Load existing schedules
            LoadSchedulesFromSettings();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets whether the schedule manager is currently running
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Gets the number of active schedules
        /// </summary>
        public int ActiveScheduleCount
        {
            get
            {
                lock (_scheduleLock)
                {
                    return _activeSchedules.Count(s => s.IsEnabled);
                }
            }
        }

        /// <summary>
        /// Gets the next scheduled notification time
        /// </summary>
        public DateTime? NextScheduledTime
        {
            get
            {
                lock (_scheduleLock)
                {
                    var nextTimes = _activeSchedules
                        .Where(s => s.IsEnabled)
                        .Select(s => s.CalculateNextTriggerTime())
                        .Where(t => t.HasValue)
                        .Select(t => t.Value)
                        .ToList();

                    return nextTimes.Any() ? nextTimes.Min() : null;
                }
            }
        }

        /// <summary>
        /// Gets a read-only list of all schedules
        /// </summary>
        public IReadOnlyList<NotificationSchedule> Schedules
        {
            get
            {
                lock (_scheduleLock)
                {
                    return _activeSchedules.ToList().AsReadOnly();
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the schedule manager
        /// </summary>
        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ScheduleManager));

            if (!_isRunning)
            {
                _isRunning = true;
                _mainTimer.Start();
                UpdateAllNextTriggerTimes();
                StatusChanged?.Invoke(this, new ScheduleManagerStatusEventArgs(true));
            }
        }

        /// <summary>
        /// Stops the schedule manager
        /// </summary>
        public void Stop()
        {
            if (_isRunning)
            {
                _isRunning = false;
                _mainTimer.Stop();
                StatusChanged?.Invoke(this, new ScheduleManagerStatusEventArgs(false));
            }
        }

        /// <summary>
        /// Adds a new schedule
        /// </summary>
        public void AddSchedule(NotificationSchedule schedule)
        {
            if (schedule == null) throw new ArgumentNullException(nameof(schedule));
            if (_disposed) throw new ObjectDisposedException(nameof(ScheduleManager));

            lock (_scheduleLock)
            {
                // Check if we've reached the maximum number of schedules
                var settings = _settingsManager.CurrentSettings.ScheduleSettings;
                if (_activeSchedules.Count >= settings.MaxSchedules)
                {
                    throw new InvalidOperationException($"Maximum number of schedules ({settings.MaxSchedules}) reached.");
                }

                // Ensure unique ID
                while (_activeSchedules.Any(s => s.Id == schedule.Id))
                {
                    schedule.Id = Guid.NewGuid();
                }

                // Calculate initial next trigger time
                schedule.NextTriggerTime = schedule.CalculateNextTriggerTime();

                _activeSchedules.Add(schedule);
                SaveSchedulesToSettings();
            }

            ScheduleAdded?.Invoke(this, new ScheduleEventArgs(schedule));
        }

        /// <summary>
        /// Removes a schedule by ID
        /// </summary>
        public bool RemoveSchedule(Guid scheduleId)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ScheduleManager));

            NotificationSchedule? removedSchedule = null;
            lock (_scheduleLock)
            {
                removedSchedule = _activeSchedules.FirstOrDefault(s => s.Id == scheduleId);
                if (removedSchedule != null)
                {
                    _activeSchedules.Remove(removedSchedule);
                    SaveSchedulesToSettings();
                }
            }

            if (removedSchedule != null)
            {
                ScheduleRemoved?.Invoke(this, new ScheduleEventArgs(removedSchedule));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates an existing schedule
        /// </summary>
        public bool UpdateSchedule(NotificationSchedule updatedSchedule)
        {
            if (updatedSchedule == null) throw new ArgumentNullException(nameof(updatedSchedule));
            if (_disposed) throw new ObjectDisposedException(nameof(ScheduleManager));

            lock (_scheduleLock)
            {
                var existingIndex = _activeSchedules.FindIndex(s => s.Id == updatedSchedule.Id);
                if (existingIndex >= 0)
                {
                    // Recalculate next trigger time
                    updatedSchedule.NextTriggerTime = updatedSchedule.CalculateNextTriggerTime();

                    _activeSchedules[existingIndex] = updatedSchedule;
                    SaveSchedulesToSettings();

                    ScheduleUpdated?.Invoke(this, new ScheduleEventArgs(updatedSchedule));
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a schedule by ID
        /// </summary>
        public NotificationSchedule? GetSchedule(Guid scheduleId)
        {
            lock (_scheduleLock)
            {
                return _activeSchedules.FirstOrDefault(s => s.Id == scheduleId);
            }
        }

        /// <summary>
        /// Enables or disables a schedule
        /// </summary>
        public bool SetScheduleEnabled(Guid scheduleId, bool enabled)
        {
            lock (_scheduleLock)
            {
                var schedule = _activeSchedules.FirstOrDefault(s => s.Id == scheduleId);
                if (schedule != null)
                {
                    schedule.IsEnabled = enabled;
                    if (enabled)
                    {
                        schedule.NextTriggerTime = schedule.CalculateNextTriggerTime();
                    }
                    else
                    {
                        schedule.NextTriggerTime = null;
                    }

                    SaveSchedulesToSettings();
                    ScheduleUpdated?.Invoke(this, new ScheduleEventArgs(schedule));
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Manually triggers a schedule
        /// </summary>
        public void TriggerSchedule(Guid scheduleId)
        {
            NotificationSchedule? schedule;
            lock (_scheduleLock)
            {
                schedule = _activeSchedules.FirstOrDefault(s => s.Id == scheduleId);
            }

            if (schedule != null)
            {
                TriggerScheduleInternal(schedule, true);
            }
        }

        /// <summary>
        /// Clears all schedules
        /// </summary>
        public void ClearAllSchedules()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ScheduleManager));

            List<NotificationSchedule> removedSchedules;
            lock (_scheduleLock)
            {
                removedSchedules = _activeSchedules.ToList();
                _activeSchedules.Clear();
                SaveSchedulesToSettings();
            }

            foreach (var schedule in removedSchedules)
            {
                ScheduleRemoved?.Invoke(this, new ScheduleEventArgs(schedule));
            }
        }

        /// <summary>
        /// Gets schedule statistics
        /// </summary>
        public ScheduleStatistics GetStatistics()
        {
            lock (_scheduleLock)
            {
                return new ScheduleStatistics
                {
                    TotalSchedules = _activeSchedules.Count,
                    EnabledSchedules = _activeSchedules.Count(s => s.IsEnabled),
                    OneTimeSchedules = _activeSchedules.Count(s => s.Recurrence == ScheduleRecurrence.OneTime),
                    RecurringSchedules = _activeSchedules.Count(s => s.Recurrence != ScheduleRecurrence.OneTime),
                    NextTriggerTime = NextScheduledTime,
                    TotalTriggers = _activeSchedules.Sum(s => s.TriggerCount)
                };
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Timer elapsed event handler
        /// </summary>
        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (!_isRunning || _disposed) return;

            try
            {
                CheckAndTriggerSchedules();
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the timer
                System.Diagnostics.Debug.WriteLine($"ScheduleManager timer error: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks for schedules that need to be triggered
        /// </summary>
        private void CheckAndTriggerSchedules()
        {
            var now = DateTime.Now;
            var schedulesToTrigger = new List<NotificationSchedule>();
            var schedulesToRemove = new List<NotificationSchedule>();

            lock (_scheduleLock)
            {
                foreach (var schedule in _activeSchedules.Where(s => s.IsEnabled).ToList())
                {
                    var nextTrigger = schedule.NextTriggerTime;
                    if (nextTrigger.HasValue && nextTrigger.Value <= now)
                    {
                        schedulesToTrigger.Add(schedule);
                    }
                }
            }

            // Trigger schedules outside of lock to avoid blocking
            foreach (var schedule in schedulesToTrigger)
            {
                TriggerScheduleInternal(schedule, false);
            }

            // Remove completed one-time schedules if configured
            var settings = _settingsManager.CurrentSettings.ScheduleSettings;
            if (settings.AutoRemoveCompletedSchedules)
            {
                lock (_scheduleLock)
                {
                    schedulesToRemove = _activeSchedules
                        .Where(s => s.Recurrence == ScheduleRecurrence.OneTime &&
                                   s.LastTriggered.HasValue &&
                                   s.CalculateNextTriggerTime() == null)
                        .ToList();

                    foreach (var schedule in schedulesToRemove)
                    {
                        _activeSchedules.Remove(schedule);
                    }

                    if (schedulesToRemove.Any())
                    {
                        SaveSchedulesToSettings();
                    }
                }

                foreach (var schedule in schedulesToRemove)
                {
                    ScheduleRemoved?.Invoke(this, new ScheduleEventArgs(schedule));
                }
            }
        }

        /// <summary>
        /// Triggers a specific schedule
        /// </summary>
        private void TriggerScheduleInternal(NotificationSchedule schedule, bool isManual)
        {
            try
            {
                // Update schedule metadata
                lock (_scheduleLock)
                {
                    schedule.LastTriggered = DateTime.Now;
                    schedule.TriggerCount++;
                    schedule.NextTriggerTime = schedule.CalculateNextTriggerTime();
                    SaveSchedulesToSettings();
                }

                // Create notification from template
                var notification = new NotificationData
                {
                    Title = schedule.NotificationTemplate.Title,
                    Message = schedule.NotificationTemplate.Message,
                    Category = schedule.NotificationTemplate.Category,
                    Priority = schedule.NotificationTemplate.Priority,
                    IconContent = schedule.NotificationTemplate.IconContent,
                    CustomTimeout = schedule.NotificationTemplate.CustomTimeout
                };

                // Add schedule metadata
                notification.Metadata["ScheduleId"] = schedule.Id;
                notification.Metadata["ScheduleName"] = schedule.Name;
                notification.Metadata["IsManualTrigger"] = isManual;
                notification.Metadata["TriggerCount"] = schedule.TriggerCount;

                // Show the notification
                _notificationManager.ShowNotification(notification);

                // Raise event
                ScheduleTriggered?.Invoke(this, new ScheduleTriggeredEventArgs(schedule, notification, isManual));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error triggering schedule '{schedule.Name}': {ex.Message}");
            }
        }

        /// <summary>
        /// Updates next trigger times for all schedules
        /// </summary>
        private void UpdateAllNextTriggerTimes()
        {
            lock (_scheduleLock)
            {
                foreach (var schedule in _activeSchedules)
                {
                    if (schedule.IsEnabled)
                    {
                        schedule.NextTriggerTime = schedule.CalculateNextTriggerTime();
                    }
                }
                SaveSchedulesToSettings();
            }
        }

        /// <summary>
        /// Loads schedules from settings
        /// </summary>
        private void LoadSchedulesFromSettings()
        {
            try
            {
                var scheduleSettings = _settingsManager.CurrentSettings.ScheduleSettings;
                lock (_scheduleLock)
                {
                    _activeSchedules.Clear();
                    _activeSchedules.AddRange(scheduleSettings.Schedules);

                    // Update next trigger times on load
                    foreach (var schedule in _activeSchedules)
                    {
                        if (schedule.IsEnabled)
                        {
                            schedule.NextTriggerTime = schedule.CalculateNextTriggerTime();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading schedules: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves schedules to settings
        /// </summary>
        private void SaveSchedulesToSettings()
        {
            try
            {
                var currentSettings = _settingsManager.CurrentSettings;
                currentSettings.ScheduleSettings.Schedules = _activeSchedules.ToList();
                _settingsManager.SaveSettingsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving schedules: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes the schedule manager
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _mainTimer?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }

    #region Event Args

    /// <summary>
    /// Event arguments for schedule-related events
    /// </summary>
    public class ScheduleEventArgs : EventArgs
    {
        public NotificationSchedule Schedule { get; }

        public ScheduleEventArgs(NotificationSchedule schedule)
        {
            Schedule = schedule;
        }
    }

    /// <summary>
    /// Event arguments for schedule triggered events
    /// </summary>
    public class ScheduleTriggeredEventArgs : EventArgs
    {
        public NotificationSchedule Schedule { get; }
        public NotificationData Notification { get; }
        public bool IsManualTrigger { get; }

        public ScheduleTriggeredEventArgs(NotificationSchedule schedule, NotificationData notification, bool isManualTrigger)
        {
            Schedule = schedule;
            Notification = notification;
            IsManualTrigger = isManualTrigger;
        }
    }

    /// <summary>
    /// Event arguments for schedule manager status events
    /// </summary>
    public class ScheduleManagerStatusEventArgs : EventArgs
    {
        public bool IsRunning { get; }

        public ScheduleManagerStatusEventArgs(bool isRunning)
        {
            IsRunning = isRunning;
        }
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Statistics about the schedule system
    /// </summary>
    public class ScheduleStatistics
    {
        public int TotalSchedules { get; set; }
        public int EnabledSchedules { get; set; }
        public int OneTimeSchedules { get; set; }
        public int RecurringSchedules { get; set; }
        public DateTime? NextTriggerTime { get; set; }
        public int TotalTriggers { get; set; }
    }

    #endregion
}