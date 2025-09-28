using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace IntervalToast
{
    /// <summary>
    /// Win32 API wrapper for global hotkey registration and management.
    /// Provides low-level keyboard hook functionality using P/Invoke.
    /// </summary>
    public static class Win32HotkeyHelper
    {
        #region Win32 API Declarations

        /// <summary>
        /// Registers a hot key with Windows
        /// </summary>
        /// <param name="hWnd">Handle to the window that will receive WM_HOTKEY messages</param>
        /// <param name="id">Unique identifier for the hot key</param>
        /// <param name="fsModifiers">Modifier keys that must be pressed</param>
        /// <param name="vk">Virtual key code</param>
        /// <returns>True if the function succeeds, otherwise false</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        /// <summary>
        /// Unregisters a hot key previously registered with RegisterHotKey
        /// </summary>
        /// <param name="hWnd">Handle to the window whose hot key is to be freed</param>
        /// <param name="id">Identifier of the hot key to be freed</param>
        /// <returns>True if the function succeeds, otherwise false</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// Gets the last Win32 error code
        /// </summary>
        /// <returns>The calling thread's last-error code</returns>
        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

        /// <summary>
        /// Formats a message string using the specified message and arguments
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int FormatMessage(
            int dwFlags,
            IntPtr lpSource,
            uint dwMessageId,
            int dwLanguageId,
            out IntPtr lpBuffer,
            int nSize,
            IntPtr Arguments);

        /// <summary>
        /// Frees memory allocated by LocalAlloc or certain other memory allocation functions
        /// </summary>
        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr hMem);

        #endregion

        #region Constants

        /// <summary>
        /// Window message sent when a registered hot key is pressed
        /// </summary>
        public const int WM_HOTKEY = 0x0312;

        /// <summary>
        /// FormatMessage flags
        /// </summary>
        private const int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;

        /// <summary>
        /// Common Win32 error codes for hotkey registration
        /// </summary>
        public static class ErrorCodes
        {
            public const uint ERROR_SUCCESS = 0;
            public const uint ERROR_HOTKEY_ALREADY_REGISTERED = 1409;
            public const uint ERROR_INVALID_PARAMETER = 87;
            public const uint ERROR_ACCESS_DENIED = 5;
            public const uint ERROR_NOT_ENOUGH_MEMORY = 8;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a global hotkey with the Windows system
        /// </summary>
        /// <param name="windowHandle">Handle to the window that will receive hotkey messages</param>
        /// <param name="hotkeyConfig">Hotkey configuration to register</param>
        /// <returns>True if registration succeeds, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when hotkeyConfig is null</exception>
        /// <exception cref="HotkeyRegistrationException">Thrown when registration fails with specific error details</exception>
        public static bool RegisterHotkey(IntPtr windowHandle, HotkeyConfiguration hotkeyConfig)
        {
            if (hotkeyConfig == null)
                throw new ArgumentNullException(nameof(hotkeyConfig));

            if (!hotkeyConfig.IsValid)
                throw new HotkeyRegistrationException("Invalid hotkey configuration", hotkeyConfig);

            // Convert WPF keys to Win32 values
            ConvertToWin32Values(hotkeyConfig);

            // Attempt registration
            bool success = RegisterHotKey(windowHandle, hotkeyConfig.Id, (uint)hotkeyConfig.Win32Modifiers, hotkeyConfig.VirtualKeyCode);

            if (!success)
            {
                uint errorCode = GetLastError();
                string errorMessage = GetErrorMessage(errorCode, hotkeyConfig);
                throw new HotkeyRegistrationException(errorMessage, hotkeyConfig, (int)errorCode);
            }

            hotkeyConfig.IsRegistered = true;
            return true;
        }

        /// <summary>
        /// Unregisters a previously registered global hotkey
        /// </summary>
        /// <param name="windowHandle">Handle to the window that registered the hotkey</param>
        /// <param name="hotkeyConfig">Hotkey configuration to unregister</param>
        /// <returns>True if unregistration succeeds, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when hotkeyConfig is null</exception>
        public static bool UnregisterHotkey(IntPtr windowHandle, HotkeyConfiguration hotkeyConfig)
        {
            if (hotkeyConfig == null)
                throw new ArgumentNullException(nameof(hotkeyConfig));

            if (!hotkeyConfig.IsRegistered)
                return true; // Already unregistered

            bool success = UnregisterHotKey(windowHandle, hotkeyConfig.Id);

            if (success)
            {
                hotkeyConfig.IsRegistered = false;
            }

            return success;
        }

        /// <summary>
        /// Unregisters multiple hotkeys in batch
        /// </summary>
        /// <param name="windowHandle">Handle to the window that registered the hotkeys</param>
        /// <param name="hotkeyConfigs">Collection of hotkey configurations to unregister</param>
        /// <returns>List of hotkeys that failed to unregister</returns>
        public static List<HotkeyConfiguration> UnregisterHotkeys(IntPtr windowHandle, IEnumerable<HotkeyConfiguration> hotkeyConfigs)
        {
            var failedUnregistrations = new List<HotkeyConfiguration>();

            foreach (var hotkey in hotkeyConfigs)
            {
                try
                {
                    if (!UnregisterHotkey(windowHandle, hotkey))
                    {
                        failedUnregistrations.Add(hotkey);
                    }
                }
                catch
                {
                    failedUnregistrations.Add(hotkey);
                }
            }

            return failedUnregistrations;
        }

        /// <summary>
        /// Checks if a hotkey combination is available for registration
        /// </summary>
        /// <param name="windowHandle">Handle to the window to test registration</param>
        /// <param name="hotkeyConfig">Hotkey configuration to test</param>
        /// <returns>True if the hotkey can be registered, false if it's already in use</returns>
        public static bool IsHotkeyAvailable(IntPtr windowHandle, HotkeyConfiguration hotkeyConfig)
        {
            if (hotkeyConfig == null || !hotkeyConfig.IsValid)
                return false;

            // Convert WPF keys to Win32 values
            ConvertToWin32Values(hotkeyConfig);

            // Try to register with a temporary ID
            int tempId = GetNextAvailableId();
            bool canRegister = RegisterHotKey(windowHandle, tempId, (uint)hotkeyConfig.Win32Modifiers, hotkeyConfig.VirtualKeyCode);

            if (canRegister)
            {
                // Clean up the test registration
                UnregisterHotKey(windowHandle, tempId);
                return true;
            }

            uint errorCode = GetLastError();
            return errorCode != ErrorCodes.ERROR_HOTKEY_ALREADY_REGISTERED;
        }

        /// <summary>
        /// Gets a list of hotkey combinations that are likely to be problematic
        /// </summary>
        /// <returns>List of hotkey configurations that should be avoided</returns>
        public static List<HotkeyConfiguration> GetProblematicHotkeys()
        {
            return new List<HotkeyConfiguration>
            {
                // System hotkeys that are typically reserved
                new() { ModifierKeys = ModifierKeys.Alt, Key = Key.Tab, Description = "Alt+Tab (system task switcher)" },
                new() { ModifierKeys = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.Delete, Description = "Ctrl+Alt+Del (system security)" },
                new() { ModifierKeys = ModifierKeys.Windows, Key = Key.L, Description = "Win+L (system lock)" },
                new() { ModifierKeys = ModifierKeys.Windows, Key = Key.R, Description = "Win+R (system run dialog)" },
                new() { ModifierKeys = ModifierKeys.Windows, Key = Key.D, Description = "Win+D (show desktop)" },
                new() { ModifierKeys = ModifierKeys.Alt, Key = Key.F4, Description = "Alt+F4 (close window)" },
                new() { ModifierKeys = ModifierKeys.Control, Key = Key.Z, Description = "Ctrl+Z (undo)" },
                new() { ModifierKeys = ModifierKeys.Control, Key = Key.Y, Description = "Ctrl+Y (redo)" },
                new() { ModifierKeys = ModifierKeys.Control, Key = Key.C, Description = "Ctrl+C (copy)" },
                new() { ModifierKeys = ModifierKeys.Control, Key = Key.V, Description = "Ctrl+V (paste)" },
                new() { ModifierKeys = ModifierKeys.Control, Key = Key.X, Description = "Ctrl+X (cut)" },
                new() { ModifierKeys = ModifierKeys.Control, Key = Key.A, Description = "Ctrl+A (select all)" },
                new() { ModifierKeys = ModifierKeys.Control, Key = Key.S, Description = "Ctrl+S (save)" },
                new() { ModifierKeys = ModifierKeys.Control, Key = Key.O, Description = "Ctrl+O (open)" },
                new() { ModifierKeys = ModifierKeys.Control, Key = Key.N, Description = "Ctrl+N (new)" }
            };
        }

        /// <summary>
        /// Validates a hotkey configuration against common conflicts
        /// </summary>
        /// <param name="hotkeyConfig">Hotkey configuration to validate</param>
        /// <returns>Validation result with warnings if any</returns>
        public static HotkeyValidationResult ValidateHotkey(HotkeyConfiguration hotkeyConfig)
        {
            var result = new HotkeyValidationResult { IsValid = true };

            if (hotkeyConfig == null)
            {
                result.IsValid = false;
                result.ErrorMessage = "Hotkey configuration is null";
                return result;
            }

            if (!hotkeyConfig.IsValid)
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid hotkey configuration";
                return result;
            }

            // Check against problematic hotkeys
            var problematicHotkeys = GetProblematicHotkeys();
            var conflicting = problematicHotkeys.FirstOrDefault(h => h.IsSameKeyCombo(hotkeyConfig));
            if (conflicting != null)
            {
                result.HasWarnings = true;
                result.Warnings.Add($"This hotkey conflicts with: {conflicting.Description}");
            }

            // Warn about hotkeys without modifiers
            if (hotkeyConfig.ModifierKeys == ModifierKeys.None && !hotkeyConfig.AllowNoModifiers)
            {
                result.HasWarnings = true;
                result.Warnings.Add("Hotkeys without modifier keys can interfere with normal typing");
            }

            // Warn about single modifier hotkeys
            if (IsSingleModifier(hotkeyConfig.ModifierKeys))
            {
                result.HasWarnings = true;
                result.Warnings.Add("Single modifier hotkeys may conflict with accessibility features");
            }

            return result;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Converts WPF ModifierKeys and Key to Win32 values
        /// </summary>
        /// <param name="hotkeyConfig">Hotkey configuration to convert</param>
        private static void ConvertToWin32Values(HotkeyConfiguration hotkeyConfig)
        {
            // Convert modifier keys
            hotkeyConfig.Win32Modifiers = Win32ModifierKeys.None;

            if (hotkeyConfig.ModifierKeys.HasFlag(ModifierKeys.Alt))
                hotkeyConfig.Win32Modifiers |= Win32ModifierKeys.Alt;

            if (hotkeyConfig.ModifierKeys.HasFlag(ModifierKeys.Control))
                hotkeyConfig.Win32Modifiers |= Win32ModifierKeys.Control;

            if (hotkeyConfig.ModifierKeys.HasFlag(ModifierKeys.Shift))
                hotkeyConfig.Win32Modifiers |= Win32ModifierKeys.Shift;

            if (hotkeyConfig.ModifierKeys.HasFlag(ModifierKeys.Windows))
                hotkeyConfig.Win32Modifiers |= Win32ModifierKeys.Windows;

            // Convert virtual key code
            hotkeyConfig.VirtualKeyCode = (uint)KeyInterop.VirtualKeyFromKey(hotkeyConfig.Key);
        }

        /// <summary>
        /// Gets a descriptive error message for hotkey registration failures
        /// </summary>
        /// <param name="errorCode">Win32 error code</param>
        /// <param name="hotkeyConfig">Hotkey configuration that failed</param>
        /// <returns>User-friendly error message</returns>
        private static string GetErrorMessage(uint errorCode, HotkeyConfiguration hotkeyConfig)
        {
            string baseMessage = $"Failed to register hotkey '{hotkeyConfig.DisplayName}'";

            return errorCode switch
            {
                ErrorCodes.ERROR_HOTKEY_ALREADY_REGISTERED =>
                    $"{baseMessage}: This key combination is already in use by another application.",

                ErrorCodes.ERROR_INVALID_PARAMETER =>
                    $"{baseMessage}: Invalid key combination or parameters.",

                ErrorCodes.ERROR_ACCESS_DENIED =>
                    $"{baseMessage}: Access denied. The application may not have sufficient privileges.",

                ErrorCodes.ERROR_NOT_ENOUGH_MEMORY =>
                    $"{baseMessage}: Insufficient system resources.",

                _ => $"{baseMessage}: System error {errorCode}. {GetSystemErrorMessage(errorCode)}"
            };
        }

        /// <summary>
        /// Gets the system error message for a Win32 error code
        /// </summary>
        /// <param name="errorCode">Win32 error code</param>
        /// <returns>System error message</returns>
        private static string GetSystemErrorMessage(uint errorCode)
        {
            try
            {
                int result = FormatMessage(
                    FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                    IntPtr.Zero,
                    errorCode,
                    0,
                    out IntPtr buffer,
                    0,
                    IntPtr.Zero);

                if (result == 0) return $"Unknown error (0x{errorCode:X})";

                string? message = Marshal.PtrToStringAnsi(buffer);
                LocalFree(buffer);

                return message?.Trim() ?? $"Unknown error (0x{errorCode:X})";
            }
            catch
            {
                return $"Unknown error (0x{errorCode:X})";
            }
        }

        /// <summary>
        /// Checks if the modifier keys represent a single modifier
        /// </summary>
        /// <param name="modifiers">Modifier keys to check</param>
        /// <returns>True if only one modifier key is set</returns>
        private static bool IsSingleModifier(ModifierKeys modifiers)
        {
            int count = 0;
            if (modifiers.HasFlag(ModifierKeys.Alt)) count++;
            if (modifiers.HasFlag(ModifierKeys.Control)) count++;
            if (modifiers.HasFlag(ModifierKeys.Shift)) count++;
            if (modifiers.HasFlag(ModifierKeys.Windows)) count++;
            return count == 1;
        }

        /// <summary>
        /// Gets the next available hotkey ID
        /// </summary>
        /// <returns>Unique hotkey ID</returns>
        private static int GetNextAvailableId()
        {
            // For testing purposes, use a high number to avoid conflicts
            return Environment.TickCount & 0x7FFF | 0x8000;
        }

        #endregion
    }

    /// <summary>
    /// Result of hotkey validation containing validation status and warnings
    /// </summary>
    public class HotkeyValidationResult
    {
        /// <summary>
        /// Whether the hotkey configuration is valid
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Whether there are warnings about the hotkey
        /// </summary>
        public bool HasWarnings { get; set; } = false;

        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// List of warning messages
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Gets all validation messages (errors and warnings)
        /// </summary>
        public List<string> AllMessages
        {
            get
            {
                var messages = new List<string>();
                if (!string.IsNullOrEmpty(ErrorMessage))
                    messages.Add($"Error: {ErrorMessage}");
                messages.AddRange(Warnings.Select(w => $"Warning: {w}"));
                return messages;
            }
        }
    }
}