using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace IntervalToast
{
    /// <summary>
    /// Win32 modifier key flags for hotkey registration
    /// </summary>
    [Flags]
    public enum Win32ModifierKeys : uint
    {
        /// <summary>No modifier key</summary>
        None = 0x0000,
        /// <summary>Alt key modifier</summary>
        Alt = 0x0001,
        /// <summary>Ctrl key modifier</summary>
        Control = 0x0002,
        /// <summary>Shift key modifier</summary>
        Shift = 0x0004,
        /// <summary>Windows key modifier</summary>
        Windows = 0x0008
    }

    /// <summary>
    /// Hotkey configuration data model containing key combination and behavior settings
    /// </summary>
    public class HotkeyConfiguration
    {
        /// <summary>
        /// Unique identifier for this hotkey
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The modifier keys (Ctrl, Alt, Shift, Win)
        /// </summary>
        public ModifierKeys ModifierKeys { get; set; } = ModifierKeys.None;

        /// <summary>
        /// The primary key
        /// </summary>
        public Key Key { get; set; } = Key.None;

        /// <summary>
        /// Win32 modifier keys for P/Invoke
        /// </summary>
        public Win32ModifierKeys Win32Modifiers { get; set; } = Win32ModifierKeys.None;

        /// <summary>
        /// Win32 virtual key code
        /// </summary>
        public uint VirtualKeyCode { get; set; }

        /// <summary>
        /// Human-readable description of the hotkey combination
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Whether this hotkey is currently enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Whether this hotkey is currently registered with the system
        /// </summary>
        public bool IsRegistered { get; set; } = false;

        /// <summary>
        /// Action to execute when hotkey is pressed
        /// </summary>
        public Action? Action { get; set; }

        /// <summary>
        /// User-friendly display name for the hotkey combination
        /// </summary>
        public string DisplayName => GenerateDisplayName();

        /// <summary>
        /// Validates that the hotkey configuration is valid
        /// </summary>
        public bool IsValid => Key != Key.None && (ModifierKeys != ModifierKeys.None || AllowNoModifiers);

        /// <summary>
        /// Whether to allow hotkeys without modifier keys (not recommended for global hotkeys)
        /// </summary>
        public bool AllowNoModifiers { get; set; } = false;

        /// <summary>
        /// Generates a human-readable display name for the hotkey combination
        /// </summary>
        private string GenerateDisplayName()
        {
            if (Key == Key.None) return "None";

            var parts = new List<string>();

            // Add modifier keys in standard order
            if (ModifierKeys.HasFlag(ModifierKeys.Control))
                parts.Add("Ctrl");
            if (ModifierKeys.HasFlag(ModifierKeys.Alt))
                parts.Add("Alt");
            if (ModifierKeys.HasFlag(ModifierKeys.Shift))
                parts.Add("Shift");
            if (ModifierKeys.HasFlag(ModifierKeys.Windows))
                parts.Add("Win");

            // Add the primary key
            parts.Add(GetFriendlyKeyName(Key));

            return string.Join(" + ", parts);
        }

        /// <summary>
        /// Gets a user-friendly name for a key
        /// </summary>
        private static string GetFriendlyKeyName(Key key)
        {
            return key switch
            {
                Key.Back => "Backspace",
                Key.Tab => "Tab",
                Key.Return => "Enter",
                Key.Space => "Space",
                Key.Prior => "Page Up",
                Key.Next => "Page Down",
                Key.End => "End",
                Key.Home => "Home",
                Key.Left => "Left Arrow",
                Key.Up => "Up Arrow",
                Key.Right => "Right Arrow",
                Key.Down => "Down Arrow",
                Key.Insert => "Insert",
                Key.Delete => "Delete",
                Key.D0 => "0",
                Key.D1 => "1",
                Key.D2 => "2",
                Key.D3 => "3",
                Key.D4 => "4",
                Key.D5 => "5",
                Key.D6 => "6",
                Key.D7 => "7",
                Key.D8 => "8",
                Key.D9 => "9",
                Key.NumPad0 => "Num 0",
                Key.NumPad1 => "Num 1",
                Key.NumPad2 => "Num 2",
                Key.NumPad3 => "Num 3",
                Key.NumPad4 => "Num 4",
                Key.NumPad5 => "Num 5",
                Key.NumPad6 => "Num 6",
                Key.NumPad7 => "Num 7",
                Key.NumPad8 => "Num 8",
                Key.NumPad9 => "Num 9",
                Key.Multiply => "Num *",
                Key.Add => "Num +",
                Key.Subtract => "Num -",
                Key.Decimal => "Num .",
                Key.Divide => "Num /",
                Key.F1 => "F1",
                Key.F2 => "F2",
                Key.F3 => "F3",
                Key.F4 => "F4",
                Key.F5 => "F5",
                Key.F6 => "F6",
                Key.F7 => "F7",
                Key.F8 => "F8",
                Key.F9 => "F9",
                Key.F10 => "F10",
                Key.F11 => "F11",
                Key.F12 => "F12",
                Key.OemSemicolon => ";",
                Key.OemPlus => "=",
                Key.OemComma => ",",
                Key.OemMinus => "-",
                Key.OemPeriod => ".",
                Key.OemQuestion => "/",
                Key.OemTilde => "`",
                Key.OemOpenBrackets => "[",
                Key.OemPipe => "\\",
                Key.OemCloseBrackets => "]",
                Key.OemQuotes => "'",
                _ => key.ToString()
            };
        }

        /// <summary>
        /// Creates a copy of this hotkey configuration
        /// </summary>
        public HotkeyConfiguration Clone()
        {
            return new HotkeyConfiguration
            {
                Id = Id,
                ModifierKeys = ModifierKeys,
                Key = Key,
                Win32Modifiers = Win32Modifiers,
                VirtualKeyCode = VirtualKeyCode,
                Description = Description,
                IsEnabled = IsEnabled,
                IsRegistered = IsRegistered,
                Action = Action,
                AllowNoModifiers = AllowNoModifiers
            };
        }

        /// <summary>
        /// Determines if two hotkey configurations represent the same key combination
        /// </summary>
        public bool IsSameKeyCombo(HotkeyConfiguration other)
        {
            if (other == null) return false;
            return ModifierKeys == other.ModifierKeys && Key == other.Key;
        }

        /// <summary>
        /// Converts this configuration to a hash code based on the key combination
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(ModifierKeys, Key);
        }

        /// <summary>
        /// Determines equality based on key combination
        /// </summary>
        public override bool Equals(object? obj)
        {
            return obj is HotkeyConfiguration other && IsSameKeyCombo(other);
        }
    }

    /// <summary>
    /// Event arguments for hotkey events
    /// </summary>
    public class HotkeyEventArgs : EventArgs
    {
        /// <summary>
        /// The hotkey configuration that was triggered
        /// </summary>
        public HotkeyConfiguration Hotkey { get; }

        /// <summary>
        /// Whether the event has been handled (prevents further processing)
        /// </summary>
        public bool Handled { get; set; } = false;

        /// <summary>
        /// Timestamp when the hotkey was pressed
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.Now;

        /// <summary>
        /// Initializes new hotkey event arguments
        /// </summary>
        public HotkeyEventArgs(HotkeyConfiguration hotkey)
        {
            Hotkey = hotkey ?? throw new ArgumentNullException(nameof(hotkey));
        }
    }

    /// <summary>
    /// Exception thrown when hotkey registration fails
    /// </summary>
    public class HotkeyRegistrationException : Exception
    {
        /// <summary>
        /// The hotkey configuration that failed to register
        /// </summary>
        public HotkeyConfiguration? Hotkey { get; }

        /// <summary>
        /// Win32 error code if available
        /// </summary>
        public int? Win32ErrorCode { get; }

        /// <summary>
        /// Initializes a new hotkey registration exception
        /// </summary>
        public HotkeyRegistrationException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new hotkey registration exception with hotkey info
        /// </summary>
        public HotkeyRegistrationException(string message, HotkeyConfiguration hotkey) : base(message)
        {
            Hotkey = hotkey;
        }

        /// <summary>
        /// Initializes a new hotkey registration exception with Win32 error
        /// </summary>
        public HotkeyRegistrationException(string message, HotkeyConfiguration hotkey, int win32ErrorCode) : base(message)
        {
            Hotkey = hotkey;
            Win32ErrorCode = win32ErrorCode;
        }

        /// <summary>
        /// Initializes a new hotkey registration exception with inner exception
        /// </summary>
        public HotkeyRegistrationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Predefined hotkey combinations for common actions
    /// </summary>
    public static class PredefinedHotkeys
    {
        /// <summary>
        /// Creates a hotkey configuration for showing notifications (Ctrl+Shift+N)
        /// </summary>
        public static HotkeyConfiguration ShowNotifications => new()
        {
            Id = 1,
            ModifierKeys = ModifierKeys.Control | ModifierKeys.Shift,
            Key = Key.N,
            Description = "Show test notification",
        };

        /// <summary>
        /// Creates a hotkey configuration for clearing all notifications (Ctrl+Shift+C)
        /// </summary>
        public static HotkeyConfiguration ClearNotifications => new()
        {
            Id = 2,
            ModifierKeys = ModifierKeys.Control | ModifierKeys.Shift,
            Key = Key.C,
            Description = "Clear all notifications",
        };

        /// <summary>
        /// Creates a hotkey configuration for toggling pause/resume (Ctrl+Shift+P)
        /// </summary>
        public static HotkeyConfiguration TogglePause => new()
        {
            Id = 3,
            ModifierKeys = ModifierKeys.Control | ModifierKeys.Shift,
            Key = Key.P,
            Description = "Toggle pause/resume notifications",
        };

        /// <summary>
        /// Creates a hotkey configuration for opening settings (Ctrl+Shift+S)
        /// </summary>
        public static HotkeyConfiguration OpenSettings => new()
        {
            Id = 4,
            ModifierKeys = ModifierKeys.Control | ModifierKeys.Shift,
            Key = Key.S,
            Description = "Open notification settings",
        };

        /// <summary>
        /// Gets all predefined hotkey configurations
        /// </summary>
        public static List<HotkeyConfiguration> GetAll() => new()
        {
            ShowNotifications,
            ClearNotifications,
            TogglePause,
            OpenSettings
        };
    }

    /// <summary>
    /// Configuration for hotkey system behavior
    /// </summary>
    public class HotkeySystemConfiguration
    {
        /// <summary>
        /// Whether global hotkeys are enabled
        /// </summary>
        public bool EnableGlobalHotkeys { get; set; } = true;

        /// <summary>
        /// Whether to show error notifications for hotkey conflicts
        /// </summary>
        public bool ShowHotkeyErrors { get; set; } = true;

        /// <summary>
        /// Maximum number of hotkeys that can be registered
        /// </summary>
        public int MaxHotkeys { get; set; } = 20;

        /// <summary>
        /// Whether to automatically retry failed registrations
        /// </summary>
        public bool AutoRetryFailedRegistrations { get; set; } = true;

        /// <summary>
        /// Delay between retry attempts in milliseconds
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Whether to log hotkey registration events for debugging
        /// </summary>
        public bool EnableLogging { get; set; } = false;

        /// <summary>
        /// Whether to allow hotkeys without modifier keys (not recommended)
        /// </summary>
        public bool AllowNoModifierKeys { get; set; } = false;
    }
}