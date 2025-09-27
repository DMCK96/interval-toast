using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace IntervalToast
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Demo application for the IntervalToast notification system Phase 1 implementation.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private readonly WindowPositionManager _positionManager;
        private int _notificationCounter = 1;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainWindow class
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _positionManager = new WindowPositionManager();

            // Initialize system information on load
            Loaded += MainWindow_Loaded;
        }

        #endregion

        #region Window Events

        /// <summary>
        /// Handles the window loaded event
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshSystemInfo();
        }

        #endregion

        #region Basic Notification Event Handlers

        /// <summary>
        /// Shows a default notification
        /// </summary>
        private void ShowDefaultBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateDefault();
            var notification = new NotificationWindow(config);

            notification.ShowNotification(
                "Interval Reminder",
                "This is a default notification with standard styling and behavior."
            );
        }

        /// <summary>
        /// Shows a notification with custom message
        /// </summary>
        private void ShowCustomMessageBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateDefault();
            var notification = new NotificationWindow(config);

            notification.ShowNotification(
                $"Custom Message #{_notificationCounter}",
                $"This is custom notification number {_notificationCounter}. It demonstrates the ability to show personalized content with different titles and messages."
            );

            _notificationCounter++;
        }

        /// <summary>
        /// Shows multiple notifications to test stacking
        /// </summary>
        private void ShowMultipleBtn_Click(object sender, RoutedEventArgs e)
        {
            var messages = new[]
            {
                ("First Notification", "This is the first notification in the stack."),
                ("Second Notification", "This is the second notification, positioned below the first."),
                ("Third Notification", "This is the third notification, demonstrating proper stacking behavior.")
            };

            foreach (var (title, message) in messages)
            {
                var config = NotificationConfiguration.CreateDefault();
                var notification = new NotificationWindow(config);
                notification.ShowNotification(title, message);

                // Small delay to demonstrate stacking
                System.Threading.Thread.Sleep(100);
            }
        }

        #endregion

        #region Themed Notification Event Handlers

        /// <summary>
        /// Shows a light theme notification
        /// </summary>
        private void ShowLightThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateLightTheme();
            var notification = new NotificationWindow(config);

            notification.ShowNotification(
                "Light Theme",
                "This notification uses the light theme configuration with bright, clean colors."
            );
        }

        /// <summary>
        /// Shows a dark theme notification
        /// </summary>
        private void ShowDarkThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateDarkTheme();
            var notification = new NotificationWindow(config);

            notification.ShowNotification(
                "Dark Theme",
                "This notification uses the dark theme configuration with darker colors and improved contrast."
            );
        }

        /// <summary>
        /// Shows a minimal style notification
        /// </summary>
        private void ShowMinimalBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateMinimal();
            var notification = new NotificationWindow(config);

            notification.ShowNotification(
                "Minimal Style",
                "Compact notification with minimal styling."
            );
        }

        /// <summary>
        /// Shows a large notification
        /// </summary>
        private void ShowLargeBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateLarge();
            var notification = new NotificationWindow(config);

            notification.ShowNotification(
                "Large Notification",
                "This is a larger notification with increased dimensions and font sizes. It's perfect for important messages that need more visibility and space for longer content."
            );
        }

        #endregion

        #region Position Testing Event Handlers

        /// <summary>
        /// Tests notification positioning based on the selected position
        /// </summary>
        private void TestPositionBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedPosition = GetSelectedPosition();
            var config = NotificationConfiguration.CreateDefault();
            config.Position = selectedPosition;

            var notification = new NotificationWindow(config);

            notification.ShowNotification(
                $"Position Test: {selectedPosition}",
                $"This notification is positioned at {selectedPosition}. It demonstrates the multi-monitor positioning system."
            );
        }

        /// <summary>
        /// Gets the selected position from the combo box
        /// </summary>
        private NotificationPosition GetSelectedPosition()
        {
            return PositionComboBox.SelectedIndex switch
            {
                0 => NotificationPosition.TopRight,
                1 => NotificationPosition.TopLeft,
                2 => NotificationPosition.BottomRight,
                3 => NotificationPosition.BottomLeft,
                4 => NotificationPosition.TopCenter,
                5 => NotificationPosition.BottomCenter,
                _ => NotificationPosition.BottomRight
            };
        }

        #endregion

        #region System Information Event Handlers

        /// <summary>
        /// Refreshes the system information display
        /// </summary>
        private void RefreshSystemInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshSystemInfo();
        }

        /// <summary>
        /// Updates the system information display
        /// </summary>
        private void RefreshSystemInfo()
        {
            try
            {
                var screens = _positionManager.GetAllScreens();
                var primaryScreen = Screen.PrimaryScreen;
                var currentScreen = _positionManager.GetTargetScreen();

                var info = new StringBuilder();
                info.AppendLine("=== DISPLAY CONFIGURATION ===");
                info.AppendLine($"Total Screens: {screens.Length}");
                info.AppendLine();

                for (int i = 0; i < screens.Length; i++)
                {
                    var screen = screens[i];
                    var isPrimary = screen.Primary;
                    var isCurrent = screen.Equals(currentScreen);

                    info.AppendLine($"Screen {i + 1}:{(isPrimary ? " [PRIMARY]" : "")}{(isCurrent ? " [CURRENT]" : "")}");
                    info.AppendLine($"  Bounds: {screen.Bounds.Width}x{screen.Bounds.Height} at ({screen.Bounds.X}, {screen.Bounds.Y})");
                    info.AppendLine($"  Working Area: {screen.WorkingArea.Width}x{screen.WorkingArea.Height} at ({screen.WorkingArea.X}, {screen.WorkingArea.Y})");
                    info.AppendLine($"  Device Name: {screen.DeviceName}");
                    info.AppendLine();
                }

                info.AppendLine("=== NOTIFICATION SYSTEM STATUS ===");
                info.AppendLine($"Current Target Screen: {Array.IndexOf(screens, currentScreen) + 1}");
                info.AppendLine($"Working Area Available: {currentScreen.WorkingArea.Width}x{currentScreen.WorkingArea.Height}");
                info.AppendLine($"Taskbar Height: {currentScreen.Bounds.Height - currentScreen.WorkingArea.Height}px");

                SystemInfoTextBlock.Text = info.ToString();
            }
            catch (Exception ex)
            {
                SystemInfoTextBlock.Text = $"Error retrieving system information: {ex.Message}";
            }
        }

        #endregion
    }
}