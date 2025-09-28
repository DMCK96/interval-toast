using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace IntervalToast
{
    /// <summary>
    /// Interaction logic for HotkeyEditWindow.xaml
    /// A dialog window for capturing and editing hotkey combinations
    /// </summary>
    public partial class HotkeyEditWindow : Window
    {
        #region Fields

        private HotkeyConfiguration? _originalHotkey;
        private HotkeyConfiguration? _capturedHotkey;
        private readonly GlobalHotkeyManager _hotkeyManager;
        private bool _isCapturing = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the edited hotkey configuration, or null if cancelled
        /// </summary>
        public HotkeyConfiguration? EditedHotkey { get; private set; }

        /// <summary>
        /// Gets whether the dialog was confirmed (not cancelled)
        /// </summary>
        public bool IsConfirmed { get; private set; } = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the HotkeyEditWindow
        /// </summary>
        /// <param name="hotkeyConfig">The current hotkey configuration to edit</param>
        /// <param name="title">The title for the hotkey being edited</param>
        public HotkeyEditWindow(HotkeyConfiguration hotkeyConfig, string title)
        {
            InitializeComponent();

            _originalHotkey = hotkeyConfig?.Clone();
            _hotkeyManager = GlobalHotkeyManager.Instance;

            // Set up the window
            HeaderText.Text = $"Edit Hotkey: {title}";
            CurrentHotkeyText.Text = _originalHotkey?.DisplayName ?? "None";

            // Set initial focus to capture area
            Loaded += (s, e) => CaptureArea.Focus();

            // Handle key events for the entire window
            KeyDown += HotkeyEditWindow_KeyDown;
            KeyUp += HotkeyEditWindow_KeyUp;

            // Make sure the window can receive key events
            Focusable = true;
        }

        /// <summary>
        /// Initializes a new instance of the HotkeyEditWindow with automatic title
        /// </summary>
        /// <param name="hotkeyConfig">The current hotkey configuration to edit</param>
        public HotkeyEditWindow(HotkeyConfiguration hotkeyConfig)
            : this(hotkeyConfig, hotkeyConfig?.Description ?? "Custom Hotkey")
        {
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles clicking on the capture area to start hotkey capture
        /// </summary>
        private void CaptureArea_Click(object sender, MouseButtonEventArgs e)
        {
            StartCapture();
        }

        /// <summary>
        /// Handles key down events for hotkey capture
        /// </summary>
        private void HotkeyEditWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_isCapturing) return;

            e.Handled = true;

            // Handle Escape to cancel
            if (e.Key == Key.Escape)
            {
                StopCapture();
                return;
            }

            // Ignore modifier-only keys
            if (IsModifierKey(e.Key)) return;

            // Capture the key combination
            var modifiers = Keyboard.Modifiers;
            var key = e.Key;

            // Create new hotkey configuration
            _capturedHotkey = new HotkeyConfiguration
            {
                ModifierKeys = modifiers,
                Key = key,
                Description = _originalHotkey?.Description ?? "Custom Hotkey",
                Action = _originalHotkey?.Action
            };

            // Update UI
            UpdateCaptureDisplay();
            ValidateCapturedHotkey();
        }

        /// <summary>
        /// Handles key up events (not used but helps with capturing)
        /// </summary>
        private void HotkeyEditWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_isCapturing)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the Test button click
        /// </summary>
        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            if (_capturedHotkey == null) return;

            try
            {
                // Show test notification
                var data = NotificationData.CreateInfo(
                    "Hotkey Test",
                    $"Test for hotkey: {_capturedHotkey.DisplayName}"
                );

                NotificationManager.Instance.ShowNotification(data);

                // Update status
                ShowMessage("Test notification sent!", "#27AE60");
            }
            catch (Exception ex)
            {
                ShowMessage($"Test failed: {ex.Message}", "#E74C3C");
            }
        }

        /// <summary>
        /// Handles the Save button click
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_capturedHotkey == null) return;

            try
            {
                // Copy original properties that should be preserved
                if (_originalHotkey != null)
                {
                    _capturedHotkey.Id = _originalHotkey.Id;
                    _capturedHotkey.Action = _originalHotkey.Action;
                    _capturedHotkey.Description = _originalHotkey.Description;
                }

                EditedHotkey = _capturedHotkey;
                IsConfirmed = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowMessage($"Save failed: {ex.Message}", "#E74C3C");
            }
        }

        /// <summary>
        /// Handles the Cancel button click
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Starts the hotkey capture process
        /// </summary>
        private void StartCapture()
        {
            _isCapturing = true;
            _capturedHotkey = null;

            // Update UI for capture mode
            CaptureArea.BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E74C3C")!);
            CaptureInstructionText.Text = "Press your desired key combination (Escape to cancel)";
            CaptureInstructionText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E74C3C")!);

            // Hide previous capture
            CapturedHotkeyText.Visibility = Visibility.Collapsed;
            ValidationMessageText.Visibility = Visibility.Collapsed;
            ConflictWarningBorder.Visibility = Visibility.Collapsed;

            // Disable buttons
            SaveButton.IsEnabled = false;
            TestButton.IsEnabled = false;

            // Focus for key capture
            Focus();
        }

        /// <summary>
        /// Stops the hotkey capture process
        /// </summary>
        private void StopCapture()
        {
            _isCapturing = false;

            // Reset UI
            CaptureArea.BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3498DB")!);
            CaptureInstructionText.Text = "Click here and press your desired key combination";
            CaptureInstructionText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#34495E")!);

            if (_capturedHotkey != null)
            {
                UpdateCaptureDisplay();
            }
        }

        /// <summary>
        /// Updates the capture display with the current hotkey
        /// </summary>
        private void UpdateCaptureDisplay()
        {
            if (_capturedHotkey == null) return;

            CapturedHotkeyText.Text = _capturedHotkey.DisplayName;
            CapturedHotkeyText.Visibility = Visibility.Visible;

            StopCapture();
        }

        /// <summary>
        /// Validates the captured hotkey and updates UI accordingly
        /// </summary>
        private void ValidateCapturedHotkey()
        {
            if (_capturedHotkey == null) return;

            ValidationMessageText.Visibility = Visibility.Collapsed;
            ConflictWarningBorder.Visibility = Visibility.Collapsed;

            // Basic validation
            if (!_capturedHotkey.IsValid)
            {
                ShowValidationMessage("Invalid hotkey combination. Please include modifier keys.", "#E74C3C");
                SaveButton.IsEnabled = false;
                TestButton.IsEnabled = false;
                return;
            }

            // Check for system conflicts
            var validation = Win32HotkeyHelper.ValidateHotkey(_capturedHotkey);
            if (!validation.IsValid)
            {
                ShowValidationMessage($"Invalid hotkey: {validation.ErrorMessage}", "#E74C3C");
                SaveButton.IsEnabled = false;
                TestButton.IsEnabled = false;
                return;
            }

            // Check for conflicts with existing hotkeys
            var isAvailable = _hotkeyManager.IsHotkeyAvailable(_capturedHotkey);
            var isSameAsOriginal = _originalHotkey?.IsSameKeyCombo(_capturedHotkey) == true;

            if (!isAvailable && !isSameAsOriginal)
            {
                var registeredHotkeys = _hotkeyManager.GetRegisteredHotkeys();
                var conflictingHotkey = registeredHotkeys.FirstOrDefault(h => h.IsSameKeyCombo(_capturedHotkey));

                if (conflictingHotkey != null)
                {
                    ShowConflictWarning($"This hotkey is already used by: {conflictingHotkey.Description}");
                }
                else
                {
                    ShowValidationMessage("This hotkey is already in use by another application.", "#E67E22");
                }

                SaveButton.IsEnabled = false;
                TestButton.IsEnabled = true; // Allow testing even with conflicts
                return;
            }

            // Show warnings if any
            if (validation.HasWarnings)
            {
                var warningMessage = string.Join(". ", validation.Warnings);
                ShowValidationMessage($"Warning: {warningMessage}", "#E67E22");
            }

            // Hotkey is valid
            SaveButton.IsEnabled = true;
            TestButton.IsEnabled = true;

            if (!validation.HasWarnings)
            {
                ShowValidationMessage("Hotkey is valid and available.", "#27AE60");
            }
        }

        /// <summary>
        /// Shows a validation message
        /// </summary>
        private void ShowValidationMessage(string message, string color)
        {
            ValidationMessageText.Text = message;
            ValidationMessageText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color)!);
            ValidationMessageText.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Shows a conflict warning
        /// </summary>
        private void ShowConflictWarning(string message)
        {
            ConflictWarningText.Text = message;
            ConflictWarningBorder.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FDF2E9")!);
            ConflictWarningBorder.BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E67E22")!);
            ConflictWarningText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E67E22")!);
            ConflictWarningBorder.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Shows a temporary message
        /// </summary>
        private void ShowMessage(string message, string color)
        {
            ShowValidationMessage(message, color);

            // Hide message after 3 seconds
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                if (ValidationMessageText.Text == message)
                {
                    ValidationMessageText.Visibility = Visibility.Collapsed;
                }
            };
            timer.Start();
        }

        /// <summary>
        /// Determines if a key is a modifier key
        /// </summary>
        private static bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LeftShift || key == Key.RightShift ||
                   key == Key.LWin || key == Key.RWin ||
                   key == Key.System;
        }

        #endregion
    }
}