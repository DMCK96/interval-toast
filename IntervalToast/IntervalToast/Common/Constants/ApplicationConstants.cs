namespace IntervalToast.Common.Constants;

/// <summary>
/// Application-wide constants
/// </summary>
public static class ApplicationConstants
{
    /// <summary>
    /// Application name
    /// </summary>
    public const string ApplicationName = "IntervalToast";

    /// <summary>
    /// Application version
    /// </summary>
    public const string Version = "2.0.0";

    /// <summary>
    /// Configuration file names
    /// </summary>
    public static class Files
    {
        public const string SettingsFileName = "IntervalToast.Settings.json";
        public const string LogFileName = "IntervalToast.log";
        public const string BackupFileExtension = ".backup";
    }

    /// <summary>
    /// Directory names
    /// </summary>
    public static class Directories
    {
        public const string ConfigurationFolderName = "IntervalToast";
        public const string LogsFolderName = "Logs";
        public const string BackupsFolderName = "Backups";
    }

    /// <summary>
    /// Default values
    /// </summary>
    public static class Defaults
    {
        public const int MaxConcurrentNotifications = 5;
        public const int DefaultDisplayDurationMs = 5000;
        public const int DefaultAnimationDurationMs = 300;
        public const int NotificationSpacing = 10;
        public const int ScreenMargin = 20;
    }

    /// <summary>
    /// Command line arguments
    /// </summary>
    public static class CommandLineArgs
    {
        public const string Minimized = "--minimized";
        public const string MinimizedShort = "/m";
        public const string ValidateHotkeys = "--validate-hotkeys";
        public const string ValidateShort = "/validate";
        public const string Help = "--help";
        public const string HelpShort = "/h";
    }

    /// <summary>
    /// Registry keys for Windows integration
    /// </summary>
    public static class Registry
    {
        public const string StartupKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        public const string ApplicationKeyName = ApplicationName;
    }

    /// <summary>
    /// System tray
    /// </summary>
    public static class SystemTray
    {
        public const string DefaultTooltip = ApplicationName + " - Notification System";
        public const string MenuItemShow = "Show Configuration";
        public const string MenuItemExit = "Exit";
        public const string MenuItemTest = "Test Notification";
    }
}