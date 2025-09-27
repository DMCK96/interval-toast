using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace IntervalToast
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Demo application for the IntervalToast notification system Phase 2 implementation.
    /// Showcases comprehensive animation system with visual effects and progress indicators.
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

        #region Animation Preset Event Handlers

        /// <summary>
        /// Shows a notification with fast animation preset
        /// </summary>
        private void ShowFastAnimationBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateDefault();
            config.ApplyAnimationPreset(AnimationPreset.Fast);

            var notification = new NotificationWindow(config);
            notification.ShowNotification(
                "Fast Animation",
                "This notification uses the Fast animation preset with quick transitions and minimal effects."
            );
        }

        /// <summary>
        /// Shows a notification with normal animation preset
        /// </summary>
        private void ShowNormalAnimationBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateDefault();
            config.ApplyAnimationPreset(AnimationPreset.Normal);

            var notification = new NotificationWindow(config);
            notification.ShowNotification(
                "Normal Animation",
                "This notification uses the Normal animation preset with balanced timing and smooth effects."
            );
        }

        /// <summary>
        /// Shows a notification with smooth animation preset
        /// </summary>
        private void ShowSmoothAnimationBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateDefault();
            config.ApplyAnimationPreset(AnimationPreset.Smooth);

            var notification = new NotificationWindow(config);
            notification.ShowNotification(
                "Smooth Animation",
                "This notification uses the Smooth animation preset with enhanced visual effects and gentle timing."
            );
        }

        /// <summary>
        /// Shows a notification with bounce animation
        /// </summary>
        private void ShowBounceAnimationBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateDefault();
            config.EntryAnimation = EntryAnimationType.BounceIn;
            config.ExitAnimation = ExitAnimationType.BounceOut;
            config.EasingFunction = AnimationEasing.Bounce;

            var notification = new NotificationWindow(config);
            notification.ShowNotification(
                "Bounce Animation",
                "This notification demonstrates bounce animations with elastic effects for a playful feel."
            );
        }

        /// <summary>
        /// Shows a notification with no animations
        /// </summary>
        private void ShowNoAnimationBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateDefault();
            config.ApplyAnimationPreset(AnimationPreset.None);

            var notification = new NotificationWindow(config);
            notification.ShowNotification(
                "No Animation",
                "This notification appears instantly without any animations for immediate visibility."
            );
        }

        #endregion

        #region Custom Animation Event Handlers

        /// <summary>
        /// Tests custom animation combinations
        /// </summary>
        private void TestCustomAnimationBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateDefault();

            // Get selected entry animation
            config.EntryAnimation = GetSelectedEntryAnimation();

            // Get selected exit animation
            config.ExitAnimation = GetSelectedExitAnimation();

            // Set custom timing
            config.AnimationDuration = TimeSpan.FromMilliseconds(600);
            config.EasingFunction = AnimationEasing.EaseInOut;

            var notification = new NotificationWindow(config);
            notification.ShowNotification(
                $"Custom Animation Test",
                $"Entry: {config.EntryAnimation}, Exit: {config.ExitAnimation}. This demonstrates custom animation combinations."
            );
        }

        /// <summary>
        /// Gets the selected entry animation from the combo box
        /// </summary>
        private EntryAnimationType GetSelectedEntryAnimation()
        {
            return EntryAnimationComboBox.SelectedIndex switch
            {
                0 => EntryAnimationType.None,
                1 => EntryAnimationType.FadeIn,
                2 => EntryAnimationType.SlideFromRight,
                3 => EntryAnimationType.SlideFromLeft,
                4 => EntryAnimationType.SlideFromTop,
                5 => EntryAnimationType.SlideFromBottom,
                6 => EntryAnimationType.ScaleUp,
                7 => EntryAnimationType.BounceIn,
                8 => EntryAnimationType.FlyIn,
                _ => EntryAnimationType.SlideFromRight
            };
        }

        /// <summary>
        /// Gets the selected exit animation from the combo box
        /// </summary>
        private ExitAnimationType GetSelectedExitAnimation()
        {
            return ExitAnimationComboBox.SelectedIndex switch
            {
                0 => ExitAnimationType.None,
                1 => ExitAnimationType.FadeOut,
                2 => ExitAnimationType.SlideToRight,
                3 => ExitAnimationType.SlideToLeft,
                4 => ExitAnimationType.SlideToTop,
                5 => ExitAnimationType.SlideToBottom,
                6 => ExitAnimationType.ScaleDown,
                7 => ExitAnimationType.BounceOut,
                8 => ExitAnimationType.FlyOut,
                _ => ExitAnimationType.SlideToRight
            };
        }

        #endregion

        #region Progress Indicator Event Handlers

        /// <summary>
        /// Tests progress indicator with selected style
        /// </summary>
        private void TestProgressBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateDefault();
            config.ProgressStyle = GetSelectedProgressStyle();
            config.ShowProgressIndicator = config.ProgressStyle != ProgressIndicatorStyle.None;
            config.AutoCloseDelay = TimeSpan.FromSeconds(8); // Longer delay to see progress

            var notification = new NotificationWindow(config);
            notification.ShowNotification(
                "Progress Indicator Test",
                $"This notification demonstrates the {config.ProgressStyle} progress indicator. Watch the progress animation!"
            );
        }

        /// <summary>
        /// Gets the selected progress style from the combo box
        /// </summary>
        private ProgressIndicatorStyle GetSelectedProgressStyle()
        {
            return ProgressStyleComboBox.SelectedIndex switch
            {
                0 => ProgressIndicatorStyle.None,
                1 => ProgressIndicatorStyle.BottomBar,
                2 => ProgressIndicatorStyle.TopBar,
                3 => ProgressIndicatorStyle.CircularCorner,
                4 => ProgressIndicatorStyle.CircularCenter,
                5 => ProgressIndicatorStyle.LeftBorder,
                6 => ProgressIndicatorStyle.RightBorder,
                _ => ProgressIndicatorStyle.BottomBar
            };
        }

        #endregion

        #region Visual Effects Event Handlers

        /// <summary>
        /// Shows a notification with glow effect
        /// </summary>
        private void ShowGlowEffectBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateDefault();
            config.EnableVisualEffects = true;
            config.GlowIntensity = 0.8;
            config.EntryAnimation = EntryAnimationType.ScaleUp;
            config.EasingFunction = AnimationEasing.Back;

            var notification = new NotificationWindow(config);
            notification.ShowNotification(
                "Glow Effect",
                "This notification features a beautiful glow effect that enhances visibility and creates visual appeal."
            );
        }

        /// <summary>
        /// Shows a notification with blur effect
        /// </summary>
        private void ShowBlurEffectBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateDefault();
            config.EnableVisualEffects = true;
            config.BlurRadius = 3.0;
            config.EntryAnimation = EntryAnimationType.FadeIn;
            config.AnimationDuration = TimeSpan.FromMilliseconds(800);

            var notification = new NotificationWindow(config);
            notification.ShowNotification(
                "Blur Effect",
                "This notification demonstrates a subtle blur effect that creates depth and modern visual styling."
            );
        }

        /// <summary>
        /// Shows a notification with enhanced hover effects
        /// </summary>
        private void ShowHoverEffectsBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateDefault();
            config.EnableHoverEffects = true;
            config.HoverScaleFactor = 1.05;
            config.EnableVisualEffects = true;
            config.AutoCloseDelay = TimeSpan.FromSeconds(10); // Longer to test hover

            var notification = new NotificationWindow(config);
            notification.ShowNotification(
                "Enhanced Hover Effects",
                "This notification has enhanced hover effects. Try hovering over it to see the smooth scaling and visual changes!"
            );
        }

        /// <summary>
        /// Shows a notification with combined visual effects
        /// </summary>
        private void ShowCombinedEffectsBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = NotificationConfiguration.CreateDefault();
            config.EnableVisualEffects = true;
            config.GlowIntensity = 0.6;
            config.BlurRadius = 2.0;
            config.EnableHoverEffects = true;
            config.HoverScaleFactor = 1.03;
            config.EntryAnimation = EntryAnimationType.FlyIn;
            config.ExitAnimation = ExitAnimationType.FlyOut;
            config.EasingFunction = AnimationEasing.Elastic;
            config.AnimationDuration = TimeSpan.FromMilliseconds(700);
            config.ShowProgressIndicator = true;
            config.ProgressStyle = ProgressIndicatorStyle.CircularCorner;

            var notification = new NotificationWindow(config);
            notification.ShowNotification(
                "Combined Effects Showcase",
                "This notification combines glow, blur, hover effects, flying animations, and progress indicators for the ultimate visual experience!"
            );
        }

        #endregion
    }
}