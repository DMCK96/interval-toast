using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace IntervalToast
{
    /// <summary>
    /// Comprehensive settings management with persistence, backup/restore, and validation
    /// </summary>
    public class SettingsManager
    {
        #region Constants

        private const string SettingsFileName = "IntervalToast.Settings.json";
        private const string BackupFileExtension = ".backup";
        private const string ConfigurationFolderName = "IntervalToast";

        #endregion

        #region Fields

        private readonly string _settingsDirectory;
        private readonly string _settingsFilePath;
        private readonly JsonSerializerOptions _jsonOptions;
        private ApplicationSettings _currentSettings;
        private readonly object _settingsLock = new object();

        #endregion

        #region Events

        /// <summary>
        /// Fired when settings are successfully loaded
        /// </summary>
        public event EventHandler<SettingsEventArgs>? SettingsLoaded;

        /// <summary>
        /// Fired when settings are successfully saved
        /// </summary>
        public event EventHandler<SettingsEventArgs>? SettingsSaved;

        /// <summary>
        /// Fired when settings fail to load or save
        /// </summary>
        public event EventHandler<SettingsErrorEventArgs>? SettingsError;

        /// <summary>
        /// Fired when specific setting values change
        /// </summary>
        public event EventHandler<SettingChangedEventArgs>? SettingChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the SettingsManager
        /// </summary>
        public SettingsManager()
        {
            // Setup settings directory in user's app data
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _settingsDirectory = Path.Combine(appDataPath, ConfigurationFolderName);
            _settingsFilePath = Path.Combine(_settingsDirectory, SettingsFileName);

            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Ensure directory exists
            EnsureSettingsDirectoryExists();

            // Initialize with default settings
            _currentSettings = ApplicationSettings.CreateDefault();

            // Attempt to load existing settings
            LoadSettingsAsync().ConfigureAwait(false);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the current application settings
        /// </summary>
        public ApplicationSettings CurrentSettings
        {
            get
            {
                lock (_settingsLock)
                {
                    return _currentSettings.Clone();
                }
            }
        }

        /// <summary>
        /// Gets the notification configuration from current settings
        /// </summary>
        public NotificationConfiguration NotificationConfiguration
        {
            get
            {
                lock (_settingsLock)
                {
                    return _currentSettings.NotificationConfiguration.Clone();
                }
            }
        }

        /// <summary>
        /// Gets the path to the settings file
        /// </summary>
        public string SettingsFilePath => _settingsFilePath;

        /// <summary>
        /// Gets the settings directory path
        /// </summary>
        public string SettingsDirectory => _settingsDirectory;

        /// <summary>
        /// Gets whether settings have been successfully loaded
        /// </summary>
        public bool IsLoaded { get; private set; }

        #endregion

        #region Loading and Saving

        /// <summary>
        /// Loads settings from disk asynchronously
        /// </summary>
        public async Task<bool> LoadSettingsAsync()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    Debug.WriteLine("Settings file not found, using defaults");
                    await SaveSettingsAsync();
                    return true;
                }

                var jsonContent = await File.ReadAllTextAsync(_settingsFilePath);
                var loadedSettings = JsonSerializer.Deserialize<ApplicationSettings>(jsonContent, _jsonOptions);

                if (loadedSettings != null)
                {
                    lock (_settingsLock)
                    {
                        _currentSettings = loadedSettings;
                        ValidateAndFixSettings();
                    }

                    IsLoaded = true;
                    SettingsLoaded?.Invoke(this, new SettingsEventArgs(_currentSettings));
                    Debug.WriteLine($"Settings loaded successfully from {_settingsFilePath}");
                    return true;
                }

                throw new InvalidOperationException("Failed to deserialize settings");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load settings: {ex.Message}");
                SettingsError?.Invoke(this, new SettingsErrorEventArgs("Load", ex));

                // Try to load from backup
                if (await LoadFromBackupAsync())
                {
                    return true;
                }

                // Use defaults if all else fails
                lock (_settingsLock)
                {
                    _currentSettings = ApplicationSettings.CreateDefault();
                }

                await SaveSettingsAsync(); // Save defaults
                return false;
            }
        }

        /// <summary>
        /// Saves current settings to disk asynchronously
        /// </summary>
        public async Task<bool> SaveSettingsAsync()
        {
            try
            {
                ApplicationSettings settingsToSave;
                lock (_settingsLock)
                {
                    ValidateAndFixSettings();
                    settingsToSave = _currentSettings.Clone();
                }

                // Create backup before saving
                await CreateBackupAsync();

                var jsonContent = JsonSerializer.Serialize(settingsToSave, _jsonOptions);
                await File.WriteAllTextAsync(_settingsFilePath, jsonContent);

                SettingsSaved?.Invoke(this, new SettingsEventArgs(settingsToSave));
                Debug.WriteLine($"Settings saved successfully to {_settingsFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save settings: {ex.Message}");
                SettingsError?.Invoke(this, new SettingsErrorEventArgs("Save", ex));
                return false;
            }
        }

        /// <summary>
        /// Loads settings synchronously (for initialization)
        /// </summary>
        public bool LoadSettings()
        {
            return LoadSettingsAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Saves settings synchronously
        /// </summary>
        public bool SaveSettings()
        {
            return SaveSettingsAsync().GetAwaiter().GetResult();
        }

        #endregion

        #region Settings Updates

        /// <summary>
        /// Updates the notification configuration
        /// </summary>
        public async Task<bool> UpdateNotificationConfigurationAsync(NotificationConfiguration newConfig)
        {
            if (newConfig == null) throw new ArgumentNullException(nameof(newConfig));

            try
            {
                lock (_settingsLock)
                {
                    var oldConfig = _currentSettings.NotificationConfiguration.Clone();
                    _currentSettings.NotificationConfiguration = newConfig.Clone();
                    _currentSettings.LastModified = DateTime.Now;

                    SettingChanged?.Invoke(this, new SettingChangedEventArgs("NotificationConfiguration", oldConfig, newConfig));
                }

                return await SaveSettingsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to update notification configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates application preferences
        /// </summary>
        public async Task<bool> UpdateApplicationPreferencesAsync(ApplicationPreferences newPreferences)
        {
            if (newPreferences == null) throw new ArgumentNullException(nameof(newPreferences));

            try
            {
                lock (_settingsLock)
                {
                    var oldPreferences = _currentSettings.ApplicationPreferences.Clone();
                    _currentSettings.ApplicationPreferences = newPreferences.Clone();
                    _currentSettings.LastModified = DateTime.Now;

                    SettingChanged?.Invoke(this, new SettingChangedEventArgs("ApplicationPreferences", oldPreferences, newPreferences));
                }

                return await SaveSettingsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to update application preferences: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates global hotkey settings
        /// </summary>
        public async Task<bool> UpdateHotkeySettingsAsync(GlobalHotkeySettings newHotkeySettings)
        {
            if (newHotkeySettings == null) throw new ArgumentNullException(nameof(newHotkeySettings));

            try
            {
                lock (_settingsLock)
                {
                    var oldSettings = _currentSettings.HotkeySettings.Clone();
                    _currentSettings.HotkeySettings = newHotkeySettings.Clone();
                    _currentSettings.LastModified = DateTime.Now;

                    SettingChanged?.Invoke(this, new SettingChangedEventArgs("HotkeySettings", oldSettings, newHotkeySettings));
                }

                return await SaveSettingsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to update hotkey settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates individual setting value
        /// </summary>
        public async Task<bool> UpdateSettingAsync<T>(string settingPath, T value)
        {
            try
            {
                lock (_settingsLock)
                {
                    var oldValue = GetSettingValue<T>(settingPath);
                    SetSettingValue(settingPath, value);
                    _currentSettings.LastModified = DateTime.Now;

                    SettingChanged?.Invoke(this, new SettingChangedEventArgs(settingPath, oldValue, value));
                }

                return await SaveSettingsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to update setting {settingPath}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Backup and Restore

        /// <summary>
        /// Creates a backup of the current settings file
        /// </summary>
        public async Task<bool> CreateBackupAsync()
        {
            try
            {
                if (!File.Exists(_settingsFilePath)) return true;

                var backupPath = _settingsFilePath + BackupFileExtension;

                // Copy current settings to backup
                await Task.Run(() => File.Copy(_settingsFilePath, backupPath, true));

                Debug.WriteLine($"Settings backup created at {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create settings backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restores settings from backup
        /// </summary>
        public async Task<bool> RestoreFromBackupAsync()
        {
            try
            {
                var backupPath = _settingsFilePath + BackupFileExtension;

                if (!File.Exists(backupPath))
                {
                    Debug.WriteLine("No backup file found");
                    return false;
                }

                // Copy backup to main settings file
                await Task.Run(() => File.Copy(backupPath, _settingsFilePath, true));

                // Reload settings
                var success = await LoadSettingsAsync();

                if (success)
                {
                    Debug.WriteLine("Settings restored from backup successfully");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to restore from backup: {ex.Message}");
                SettingsError?.Invoke(this, new SettingsErrorEventArgs("Restore", ex));
                return false;
            }
        }

        /// <summary>
        /// Loads settings from backup file
        /// </summary>
        private async Task<bool> LoadFromBackupAsync()
        {
            try
            {
                var backupPath = _settingsFilePath + BackupFileExtension;

                if (!File.Exists(backupPath)) return false;

                var jsonContent = await File.ReadAllTextAsync(backupPath);
                var loadedSettings = JsonSerializer.Deserialize<ApplicationSettings>(jsonContent, _jsonOptions);

                if (loadedSettings != null)
                {
                    lock (_settingsLock)
                    {
                        _currentSettings = loadedSettings;
                        ValidateAndFixSettings();
                    }

                    IsLoaded = true;
                    Debug.WriteLine("Settings loaded from backup");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load from backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Exports settings to a specified file
        /// </summary>
        public async Task<bool> ExportSettingsAsync(string filePath)
        {
            try
            {
                ApplicationSettings settingsToExport;
                lock (_settingsLock)
                {
                    settingsToExport = _currentSettings.Clone();
                }

                var jsonContent = JsonSerializer.Serialize(settingsToExport, _jsonOptions);
                await File.WriteAllTextAsync(filePath, jsonContent);

                Debug.WriteLine($"Settings exported to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to export settings: {ex.Message}");
                SettingsError?.Invoke(this, new SettingsErrorEventArgs("Export", ex));
                return false;
            }
        }

        /// <summary>
        /// Imports settings from a specified file
        /// </summary>
        public async Task<bool> ImportSettingsAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Settings file not found: {filePath}");
                }

                var jsonContent = await File.ReadAllTextAsync(filePath);
                var importedSettings = JsonSerializer.Deserialize<ApplicationSettings>(jsonContent, _jsonOptions);

                if (importedSettings != null)
                {
                    lock (_settingsLock)
                    {
                        _currentSettings = importedSettings;
                        ValidateAndFixSettings();
                        _currentSettings.LastModified = DateTime.Now;
                    }

                    var success = await SaveSettingsAsync();
                    if (success)
                    {
                        SettingsLoaded?.Invoke(this, new SettingsEventArgs(_currentSettings));
                        Debug.WriteLine($"Settings imported from {filePath}");
                    }

                    return success;
                }

                throw new InvalidOperationException("Failed to deserialize imported settings");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to import settings: {ex.Message}");
                SettingsError?.Invoke(this, new SettingsErrorEventArgs("Import", ex));
                return false;
            }
        }

        #endregion

        #region Reset and Defaults

        /// <summary>
        /// Resets all settings to default values
        /// </summary>
        public async Task<bool> ResetToDefaultsAsync()
        {
            try
            {
                ApplicationSettings oldSettings;
                lock (_settingsLock)
                {
                    oldSettings = _currentSettings.Clone();
                    _currentSettings = ApplicationSettings.CreateDefault();
                }

                var success = await SaveSettingsAsync();
                if (success)
                {
                    SettingChanged?.Invoke(this, new SettingChangedEventArgs("All", oldSettings, _currentSettings));
                    Debug.WriteLine("Settings reset to defaults");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to reset settings: {ex.Message}");
                SettingsError?.Invoke(this, new SettingsErrorEventArgs("Reset", ex));
                return false;
            }
        }

        /// <summary>
        /// Resets only notification configuration to defaults
        /// </summary>
        public async Task<bool> ResetNotificationConfigurationAsync()
        {
            var defaultConfig = NotificationConfiguration.CreateDefault();
            return await UpdateNotificationConfigurationAsync(defaultConfig);
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates and fixes settings to ensure they are within acceptable ranges
        /// </summary>
        private void ValidateAndFixSettings()
        {
            try
            {
                // Validate notification configuration
                if (_currentSettings.NotificationConfiguration != null)
                {
                    if (!_currentSettings.NotificationConfiguration.IsValid())
                    {
                        Debug.WriteLine("Invalid notification configuration detected, applying fixes");
                        _currentSettings.NotificationConfiguration.ClampValues();
                    }
                }
                else
                {
                    _currentSettings.NotificationConfiguration = NotificationConfiguration.CreateDefault();
                }

                // Validate application preferences
                if (_currentSettings.ApplicationPreferences == null)
                {
                    _currentSettings.ApplicationPreferences = ApplicationPreferences.CreateDefault();
                }

                // Validate hotkey settings
                if (_currentSettings.HotkeySettings == null)
                {
                    _currentSettings.HotkeySettings = GlobalHotkeySettings.CreateDefault();
                }

                // Validate statistics
                if (_currentSettings.Statistics == null)
                {
                    _currentSettings.Statistics = NotificationStatistics.CreateDefault();
                }

                // Ensure created date is set
                if (_currentSettings.CreatedDate == default)
                {
                    _currentSettings.CreatedDate = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error validating settings: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Ensures the settings directory exists
        /// </summary>
        private void EnsureSettingsDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_settingsDirectory))
                {
                    Directory.CreateDirectory(_settingsDirectory);
                    Debug.WriteLine($"Created settings directory: {_settingsDirectory}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create settings directory: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets a setting value using a dot-notation path
        /// </summary>
        private T? GetSettingValue<T>(string settingPath)
        {
            // Simple implementation for common paths
            // In a full implementation, you'd use reflection or expression trees
            var parts = settingPath.Split('.');

            switch (parts[0].ToLowerInvariant())
            {
                case "notificationconfiguration" when parts.Length > 1:
                    return GetNotificationConfigProperty<T>(parts[1]);
                case "applicationpreferences" when parts.Length > 1:
                    return GetApplicationPreferencesProperty<T>(parts[1]);
                default:
                    return default(T);
            }
        }

        /// <summary>
        /// Sets a setting value using a dot-notation path
        /// </summary>
        private void SetSettingValue<T>(string settingPath, T value)
        {
            // Simple implementation for common paths
            var parts = settingPath.Split('.');

            switch (parts[0].ToLowerInvariant())
            {
                case "notificationconfiguration" when parts.Length > 1:
                    SetNotificationConfigProperty(parts[1], value);
                    break;
                case "applicationpreferences" when parts.Length > 1:
                    SetApplicationPreferencesProperty(parts[1], value);
                    break;
            }
        }

        private T? GetNotificationConfigProperty<T>(string propertyName)
        {
            var config = _currentSettings.NotificationConfiguration;
            return propertyName.ToLowerInvariant() switch
            {
                "width" => (T)(object)config.Width,
                "height" => (T)(object)config.Height,
                "autocloseDelay" => (T)(object)config.AutoCloseDelay,
                _ => default(T)
            };
        }

        private void SetNotificationConfigProperty<T>(string propertyName, T value)
        {
            var config = _currentSettings.NotificationConfiguration;
            switch (propertyName.ToLowerInvariant())
            {
                case "width" when value is double width:
                    config.Width = width;
                    break;
                case "height" when value is double height:
                    config.Height = height;
                    break;
                case "autocloseDelay" when value is TimeSpan delay:
                    config.AutoCloseDelay = delay;
                    break;
            }
        }

        private T? GetApplicationPreferencesProperty<T>(string propertyName)
        {
            var prefs = _currentSettings.ApplicationPreferences;
            return propertyName.ToLowerInvariant() switch
            {
                "startwithwindows" => (T)(object)prefs.StartWithWindows,
                "minimizeToTray" => (T)(object)prefs.MinimizeToTray,
                _ => default(T)
            };
        }

        private void SetApplicationPreferencesProperty<T>(string propertyName, T value)
        {
            var prefs = _currentSettings.ApplicationPreferences;
            switch (propertyName.ToLowerInvariant())
            {
                case "startwithwindows" when value is bool startWith:
                    prefs.StartWithWindows = startWith;
                    break;
                case "minimizeToTray" when value is bool minimize:
                    prefs.MinimizeToTray = minimize;
                    break;
            }
        }

        #endregion
    }

    #region Settings Data Models

    /// <summary>
    /// Complete application settings container
    /// </summary>
    public class ApplicationSettings
    {
        /// <summary>
        /// Application version that created these settings
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// When these settings were first created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// When these settings were last modified
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.Now;

        /// <summary>
        /// Notification system configuration
        /// </summary>
        public NotificationConfiguration NotificationConfiguration { get; set; } = NotificationConfiguration.CreateDefault();

        /// <summary>
        /// Application-wide preferences
        /// </summary>
        public ApplicationPreferences ApplicationPreferences { get; set; } = ApplicationPreferences.CreateDefault();

        /// <summary>
        /// Global hotkey settings
        /// </summary>
        public GlobalHotkeySettings HotkeySettings { get; set; } = GlobalHotkeySettings.CreateDefault();

        /// <summary>
        /// Usage statistics
        /// </summary>
        public NotificationStatistics Statistics { get; set; } = NotificationStatistics.CreateDefault();

        /// <summary>
        /// Notification scheduling settings
        /// </summary>
        public ScheduleSettings ScheduleSettings { get; set; } = ScheduleSettings.CreateDefault();

        /// <summary>
        /// Creates default application settings
        /// </summary>
        public static ApplicationSettings CreateDefault() => new ApplicationSettings();

        /// <summary>
        /// Creates a deep copy of the settings
        /// </summary>
        public ApplicationSettings Clone()
        {
            return new ApplicationSettings
            {
                Version = Version,
                CreatedDate = CreatedDate,
                LastModified = LastModified,
                NotificationConfiguration = NotificationConfiguration.Clone(),
                ApplicationPreferences = ApplicationPreferences.Clone(),
                HotkeySettings = HotkeySettings.Clone(),
                Statistics = Statistics.Clone(),
                ScheduleSettings = ScheduleSettings.Clone()
            };
        }
    }

    /// <summary>
    /// Application-wide preferences and behavior settings
    /// </summary>
    public class ApplicationPreferences
    {
        /// <summary>
        /// Whether to start with Windows
        /// </summary>
        public bool StartWithWindows { get; set; } = false;

        /// <summary>
        /// Whether to minimize to system tray instead of closing
        /// </summary>
        public bool MinimizeToTray { get; set; } = true;

        /// <summary>
        /// Whether to show the configuration window on startup
        /// </summary>
        public bool ShowConfigurationOnStartup { get; set; } = false;

        /// <summary>
        /// Whether to enable automatic updates
        /// </summary>
        public bool EnableAutomaticUpdates { get; set; } = true;

        /// <summary>
        /// Whether to collect anonymous usage statistics
        /// </summary>
        public bool CollectUsageStatistics { get; set; } = true;

        /// <summary>
        /// Theme preference for the configuration interface
        /// </summary>
        public string InterfaceTheme { get; set; } = "Auto"; // Auto, Light, Dark

        /// <summary>
        /// Language/locale preference
        /// </summary>
        public string Language { get; set; } = "en-US";

        /// <summary>
        /// Creates default application preferences
        /// </summary>
        public static ApplicationPreferences CreateDefault() => new ApplicationPreferences();

        /// <summary>
        /// Creates a copy of the preferences
        /// </summary>
        public ApplicationPreferences Clone()
        {
            return new ApplicationPreferences
            {
                StartWithWindows = StartWithWindows,
                MinimizeToTray = MinimizeToTray,
                ShowConfigurationOnStartup = ShowConfigurationOnStartup,
                EnableAutomaticUpdates = EnableAutomaticUpdates,
                CollectUsageStatistics = CollectUsageStatistics,
                InterfaceTheme = InterfaceTheme,
                Language = Language
            };
        }
    }

    /// <summary>
    /// Global hotkey configuration settings
    /// </summary>
    public class GlobalHotkeySettings
    {
        /// <summary>
        /// Whether global hotkeys are enabled
        /// </summary>
        public bool EnableGlobalHotkeys { get; set; } = true;

        /// <summary>
        /// Hotkey definitions
        /// </summary>
        public Dictionary<string, string> HotkeyDefinitions { get; set; } = GetDefaultHotkeys();

        /// <summary>
        /// Whether to show feedback notifications for hotkey actions
        /// </summary>
        public bool ShowHotkeyFeedback { get; set; } = true;

        /// <summary>
        /// Gets default hotkey definitions
        /// </summary>
        private static Dictionary<string, string> GetDefaultHotkeys() => new Dictionary<string, string>
        {
            ["DismissAll"] = "Ctrl+Shift+D",
            ["DismissLatest"] = "Ctrl+Shift+Esc",
            ["ToggleSystem"] = "Ctrl+Shift+P",
            ["ShowQueueStatus"] = "Ctrl+Shift+Q"
        };

        /// <summary>
        /// Creates default hotkey settings
        /// </summary>
        public static GlobalHotkeySettings CreateDefault() => new GlobalHotkeySettings();

        /// <summary>
        /// Creates a copy of the hotkey settings
        /// </summary>
        public GlobalHotkeySettings Clone()
        {
            return new GlobalHotkeySettings
            {
                EnableGlobalHotkeys = EnableGlobalHotkeys,
                HotkeyDefinitions = new Dictionary<string, string>(HotkeyDefinitions),
                ShowHotkeyFeedback = ShowHotkeyFeedback
            };
        }
    }

    /// <summary>
    /// Usage statistics and analytics data
    /// </summary>
    public class NotificationStatistics
    {
        /// <summary>
        /// Total notifications shown since installation
        /// </summary>
        public long TotalNotificationsShown { get; set; } = 0;

        /// <summary>
        /// Notifications shown by category
        /// </summary>
        public Dictionary<NotificationCategory, long> NotificationsByCategory { get; set; } = new Dictionary<NotificationCategory, long>();

        /// <summary>
        /// Notifications shown by priority
        /// </summary>
        public Dictionary<NotificationPriority, long> NotificationsByPriority { get; set; } = new Dictionary<NotificationPriority, long>();

        /// <summary>
        /// Average notification display time
        /// </summary>
        public TimeSpan AverageDisplayTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Most active notification time period
        /// </summary>
        public TimeSpan MostActiveHour { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Date of first recorded notification
        /// </summary>
        public DateTime FirstNotificationDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Date of last recorded notification
        /// </summary>
        public DateTime LastNotificationDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Creates default statistics
        /// </summary>
        public static NotificationStatistics CreateDefault() => new NotificationStatistics();

        /// <summary>
        /// Creates a copy of the statistics
        /// </summary>
        public NotificationStatistics Clone()
        {
            return new NotificationStatistics
            {
                TotalNotificationsShown = TotalNotificationsShown,
                NotificationsByCategory = new Dictionary<NotificationCategory, long>(NotificationsByCategory),
                NotificationsByPriority = new Dictionary<NotificationPriority, long>(NotificationsByPriority),
                AverageDisplayTime = AverageDisplayTime,
                MostActiveHour = MostActiveHour,
                FirstNotificationDate = FirstNotificationDate,
                LastNotificationDate = LastNotificationDate
            };
        }
    }

    #endregion

    #region Event Args

    /// <summary>
    /// Event arguments for settings events
    /// </summary>
    public class SettingsEventArgs : EventArgs
    {
        public ApplicationSettings Settings { get; }

        public SettingsEventArgs(ApplicationSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
    }

    /// <summary>
    /// Event arguments for settings errors
    /// </summary>
    public class SettingsErrorEventArgs : EventArgs
    {
        public string Operation { get; }
        public Exception Exception { get; }

        public SettingsErrorEventArgs(string operation, Exception exception)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }
    }

    /// <summary>
    /// Event arguments for individual setting changes
    /// </summary>
    public class SettingChangedEventArgs : EventArgs
    {
        public string SettingName { get; }
        public object? OldValue { get; }
        public object? NewValue { get; }

        public SettingChangedEventArgs(string settingName, object? oldValue, object? newValue)
        {
            SettingName = settingName ?? throw new ArgumentNullException(nameof(settingName));
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    #endregion
}