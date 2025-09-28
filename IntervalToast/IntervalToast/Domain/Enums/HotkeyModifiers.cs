namespace IntervalToast.Domain.Enums;

/// <summary>
/// Defines hotkey modifier keys
/// </summary>
[Flags]
public enum HotkeyModifiers
{
    /// <summary>No modifier keys</summary>
    None = 0,

    /// <summary>Alt key</summary>
    Alt = 1,

    /// <summary>Control key</summary>
    Control = 2,

    /// <summary>Shift key</summary>
    Shift = 4,

    /// <summary>Windows key</summary>
    Windows = 8
}