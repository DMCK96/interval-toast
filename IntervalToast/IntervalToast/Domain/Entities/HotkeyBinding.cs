using IntervalToast.Domain.ValueObjects;

namespace IntervalToast.Domain.Entities;

/// <summary>
/// Domain entity representing a hotkey binding
/// </summary>
public sealed class HotkeyBinding
{
    /// <summary>
    /// Gets the unique identifier for this hotkey binding
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the name/description of this hotkey binding
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the hotkey definition
    /// </summary>
    public HotkeyDefinition HotkeyDefinition { get; private set; }

    /// <summary>
    /// Gets the action to execute when the hotkey is pressed
    /// </summary>
    public string Action { get; private set; }

    /// <summary>
    /// Gets whether this hotkey binding is enabled
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Gets whether this hotkey is currently registered with the system
    /// </summary>
    public bool IsRegistered { get; private set; }

    /// <summary>
    /// Gets the creation timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the last time this hotkey was used
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; private set; }

    /// <summary>
    /// Gets the usage count
    /// </summary>
    public int UsageCount { get; private set; }

    /// <summary>
    /// Creates a new hotkey binding
    /// </summary>
    /// <param name="name">The name/description of the binding</param>
    /// <param name="hotkeyDefinition">The hotkey definition</param>
    /// <param name="action">The action to execute</param>
    /// <param name="isEnabled">Whether the binding is enabled</param>
    public HotkeyBinding(string name, HotkeyDefinition hotkeyDefinition, string action, bool isEnabled = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be null or empty", nameof(action));

        Id = Guid.NewGuid();
        Name = name;
        HotkeyDefinition = hotkeyDefinition ?? throw new ArgumentNullException(nameof(hotkeyDefinition));
        Action = action;
        IsEnabled = isEnabled;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the binding name
    /// </summary>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        Name = name;
    }

    /// <summary>
    /// Updates the hotkey definition
    /// </summary>
    public void UpdateHotkey(HotkeyDefinition hotkeyDefinition)
    {
        HotkeyDefinition = hotkeyDefinition ?? throw new ArgumentNullException(nameof(hotkeyDefinition));
    }

    /// <summary>
    /// Updates the action
    /// </summary>
    public void UpdateAction(string action)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be null or empty", nameof(action));

        Action = action;
    }

    /// <summary>
    /// Enables the hotkey binding
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
    }

    /// <summary>
    /// Disables the hotkey binding
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
    }

    /// <summary>
    /// Marks the hotkey as registered with the system
    /// </summary>
    public void MarkAsRegistered()
    {
        IsRegistered = true;
    }

    /// <summary>
    /// Marks the hotkey as unregistered from the system
    /// </summary>
    public void MarkAsUnregistered()
    {
        IsRegistered = false;
    }

    /// <summary>
    /// Records usage of this hotkey
    /// </summary>
    public void RecordUsage()
    {
        UsageCount++;
        LastUsedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets whether this hotkey binding is valid and can be registered
    /// </summary>
    public bool CanBeRegistered => IsEnabled && HotkeyDefinition.IsValid;
}