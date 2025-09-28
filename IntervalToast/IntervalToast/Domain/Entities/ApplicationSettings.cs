using IntervalToast.Domain.ValueObjects;

namespace IntervalToast.Domain.Entities;

/// <summary>
/// Domain entity representing application settings
/// </summary>
public sealed class ApplicationSettings
{
    /// <summary>
    /// Gets the general application preferences
    /// </summary>
    public ApplicationPreferences Preferences { get; private set; }

    /// <summary>
    /// Gets the default notification style
    /// </summary>
    public NotificationStyle DefaultNotificationStyle { get; private set; }

    /// <summary>
    /// Gets the notification display settings
    /// </summary>
    public NotificationDisplaySettings DisplaySettings { get; private set; }

    /// <summary>
    /// Gets the hotkey bindings
    /// </summary>
    public List<HotkeyBinding> HotkeyBindings { get; private set; }

    /// <summary>
    /// Gets the last time settings were updated
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; private set; }

    /// <summary>
    /// Creates new application settings with defaults
    /// </summary>
    public ApplicationSettings()
    {
        Preferences = new ApplicationPreferences();
        DefaultNotificationStyle = new NotificationStyle();
        DisplaySettings = new NotificationDisplaySettings();
        HotkeyBindings = new List<HotkeyBinding>();
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the application preferences
    /// </summary>
    public void UpdatePreferences(ApplicationPreferences preferences)
    {
        Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the default notification style
    /// </summary>
    public void UpdateDefaultStyle(NotificationStyle style)
    {
        DefaultNotificationStyle = style ?? throw new ArgumentNullException(nameof(style));
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the display settings
    /// </summary>
    public void UpdateDisplaySettings(NotificationDisplaySettings settings)
    {
        DisplaySettings = settings ?? throw new ArgumentNullException(nameof(settings));
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Adds a hotkey binding
    /// </summary>
    public void AddHotkeyBinding(HotkeyBinding binding)
    {
        if (binding == null)
            throw new ArgumentNullException(nameof(binding));

        // Check for duplicate hotkey definitions
        if (HotkeyBindings.Any(b => b.HotkeyDefinition.Equals(binding.HotkeyDefinition)))
            throw new InvalidOperationException("A hotkey binding with this key combination already exists");

        HotkeyBindings.Add(binding);
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Removes a hotkey binding
    /// </summary>
    public bool RemoveHotkeyBinding(Guid bindingId)
    {
        var binding = HotkeyBindings.FirstOrDefault(b => b.Id == bindingId);
        if (binding == null)
            return false;

        HotkeyBindings.Remove(binding);
        LastUpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    /// <summary>
    /// Gets a hotkey binding by ID
    /// </summary>
    public HotkeyBinding? GetHotkeyBinding(Guid bindingId)
    {
        return HotkeyBindings.FirstOrDefault(b => b.Id == bindingId);
    }

    /// <summary>
    /// Gets enabled hotkey bindings
    /// </summary>
    public IEnumerable<HotkeyBinding> GetEnabledHotkeyBindings()
    {
        return HotkeyBindings.Where(b => b.IsEnabled);
    }
}

/// <summary>
/// Value object for application preferences
/// </summary>
public sealed record ApplicationPreferences
{
    /// <summary>
    /// Gets whether to start with Windows
    /// </summary>
    public bool StartWithWindows { get; init; } = false;

    /// <summary>
    /// Gets whether to start minimized to system tray
    /// </summary>
    public bool StartMinimized { get; init; } = true;

    /// <summary>
    /// Gets whether to show configuration dashboard on startup
    /// </summary>
    public bool ShowConfigurationOnStartup { get; init; } = false;

    /// <summary>
    /// Gets whether to enable sound notifications
    /// </summary>
    public bool EnableSounds { get; init; } = false;

    /// <summary>
    /// Gets whether to use animations
    /// </summary>
    public bool EnableAnimations { get; init; } = true;

    /// <summary>
    /// Gets the theme preference
    /// </summary>
    public string Theme { get; init; } = "System";

    /// <summary>
    /// Gets the language/locale preference
    /// </summary>
    public string Language { get; init; } = "en-US";
}

/// <summary>
/// Value object for notification display settings
/// </summary>
public sealed record NotificationDisplaySettings
{
    /// <summary>
    /// Gets the maximum number of notifications to display simultaneously
    /// </summary>
    public int MaxConcurrentNotifications { get; init; } = 5;

    /// <summary>
    /// Gets the default display duration in milliseconds
    /// </summary>
    public int DefaultDisplayDurationMs { get; init; } = 5000;

    /// <summary>
    /// Gets the screen edge for notification positioning
    /// </summary>
    public string ScreenEdge { get; init; } = "BottomRight";

    /// <summary>
    /// Gets the margin from screen edge in pixels
    /// </summary>
    public int ScreenMargin { get; init; } = 20;

    /// <summary>
    /// Gets the spacing between notifications in pixels
    /// </summary>
    public int NotificationSpacing { get; init; } = 10;

    /// <summary>
    /// Gets whether notifications should stay on top of other windows
    /// </summary>
    public bool StayOnTop { get; init; } = true;

    /// <summary>
    /// Gets whether to enable notification grouping by category
    /// </summary>
    public bool EnableGrouping { get; init; } = false;
}