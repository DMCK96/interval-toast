using IntervalToast.Domain.Enums;

namespace IntervalToast.Domain.ValueObjects;

/// <summary>
/// Value object representing a hotkey definition
/// </summary>
public sealed record HotkeyDefinition
{
    /// <summary>
    /// Gets the modifier keys for the hotkey
    /// </summary>
    public HotkeyModifiers Modifiers { get; init; }

    /// <summary>
    /// Gets the key code for the hotkey
    /// </summary>
    public int KeyCode { get; init; }

    /// <summary>
    /// Gets a human-readable description of the hotkey
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Creates a new hotkey definition
    /// </summary>
    /// <param name="modifiers">The modifier keys</param>
    /// <param name="keyCode">The key code</param>
    /// <param name="description">Human-readable description</param>
    public HotkeyDefinition(HotkeyModifiers modifiers, int keyCode, string description = "")
    {
        Modifiers = modifiers;
        KeyCode = keyCode;
        Description = description;
    }

    /// <summary>
    /// Gets a string representation of the hotkey
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>();

        if (Modifiers.HasFlag(HotkeyModifiers.Control))
            parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
            parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
            parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Windows))
            parts.Add("Win");

        parts.Add($"Key{KeyCode}");

        return string.Join(" + ", parts);
    }

    /// <summary>
    /// Validates if the hotkey definition is valid
    /// </summary>
    public bool IsValid => KeyCode > 0 && Modifiers != HotkeyModifiers.None;
}