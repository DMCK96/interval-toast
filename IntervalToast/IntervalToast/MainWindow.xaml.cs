using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

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
        private readonly NotificationManager _notificationManager;
        private readonly GlobalHotkeyManager _hotkeyManager;
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
            _notificationManager = NotificationManager.Instance;
            _hotkeyManager = GlobalHotkeyManager.Instance;

            // Configure Phase 3 features
            ConfigurePhase3Features();

            // Initialize global hotkey system
            InitializeHotkeySystem();

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
            InitializeHotkeyUI();

            // Run comprehensive hotkey validation tests after a short delay
            Dispatcher.BeginInvoke(async () =>
            {
                await Task.Delay(1000); // Wait for UI to fully initialize
                RunHotkeyValidationTests();
            });
        }

        #endregion

        #region Hotkey System Initialization

        /// <summary>
        /// Initializes the global hotkey system with event handlers
        /// </summary>
        private void InitializeHotkeySystem()
        {
            try
            {
                // Subscribe to hotkey events
                _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
                _hotkeyManager.HotkeyRegistered += OnHotkeyRegistered;
                _hotkeyManager.HotkeyRegistrationFailed += OnHotkeyRegistrationFailed;
                _hotkeyManager.SystemStateChanged += OnHotkeySystemStateChanged;

                // Configure hotkey system
                var config = new HotkeySystemConfiguration
                {
                    EnableGlobalHotkeys = true,
                    ShowHotkeyErrors = true,
                    AutoRetryFailedRegistrations = true,
                    EnableLogging = true
                };
                _hotkeyManager.SetSystemConfiguration(config);

                System.Diagnostics.Debug.WriteLine("Global hotkey system initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing hotkey system: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles hotkey press events
        /// </summary>
        private void OnHotkeyPressed(object? sender, HotkeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Hotkey pressed: {e.Hotkey.DisplayName}");

            // Show feedback notification
            var data = NotificationData.CreateSystem(
                "Hotkey Activated",
                $"Executed: {e.Hotkey.Description}"
            );
            _notificationManager.ShowNotification(data);
        }

        /// <summary>
        /// Handles successful hotkey registration
        /// </summary>
        private void OnHotkeyRegistered(object? sender, HotkeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Hotkey registered successfully: {e.Hotkey.DisplayName}");

            // Update UI to reflect successful registration
            Dispatcher.BeginInvoke(() => RefreshHotkeyStatus());
        }

        /// <summary>
        /// Handles hotkey registration failures
        /// </summary>
        private void OnHotkeyRegistrationFailed(object? sender, HotkeyRegistrationFailedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Hotkey registration failed: {e.Hotkey.DisplayName} - {e.Exception.Message}");

            // Update UI to reflect failed registration
            Dispatcher.BeginInvoke(() => RefreshHotkeyStatus());
        }

        /// <summary>
        /// Handles hotkey system state changes
        /// </summary>
        private void OnHotkeySystemStateChanged(object? sender, HotkeySystemStateChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Hotkey system state changed: {(e.IsEnabled ? "Enabled" : "Disabled")}");

            // Update UI to reflect system state change
            Dispatcher.BeginInvoke(() => UpdateHotkeySystemStatus());

            var data = NotificationData.CreateInfo(
                "Hotkey System",
                $"Global hotkeys are now {(e.IsEnabled ? "enabled" : "disabled")}"
            );
            _notificationManager.ShowNotification(data);
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
        /// Shows multiple notifications to test vertical stacking
        /// </summary>
        private void ShowMultipleBtn_Click(object sender, RoutedEventArgs e)
        {
            var messages = new[]
            {
                ("First Notification", "This is the first notification in the vertical stack."),
                ("Second Notification", "This is the second notification, positioned vertically below the first."),
                ("Third Notification", "This is the third notification, demonstrating proper vertical stacking behavior."),
                ("Fourth Notification", "This is the fourth notification, continuing the vertical stack."),
                ("Fifth Notification", "This is the fifth notification, completing the vertical stack.")
            };

            foreach (var (title, message) in messages)
            {
                var data = new NotificationData
                {
                    Title = title,
                    Message = message,
                    Category = NotificationCategory.Info,
                    Priority = NotificationPriority.Normal
                };
                _notificationManager.ShowNotification(data);

                // Small delay to demonstrate stacking
                System.Threading.Thread.Sleep(300);
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

        #region Phase 3: Configuration and Category/Priority Event Handlers

        /// <summary>
        /// Configures Phase 3 advanced features
        /// </summary>
        private void ConfigurePhase3Features()
        {
            var config = NotificationConfiguration.CreateDefault();

            // Enable Phase 3 features
            config.EnablePriorityOrdering = true;
            config.EnableIntelligentSpacing = true;
            config.EnableDynamicSizing = true;
            config.EnableSmartPositioning = true;
            config.EnableCompactMode = true;
            config.CompactModeThreshold = 3;
            config.EnableSummaryNotifications = true;
            config.MaxVisibleNotifications = 5;
            config.ShowCategoryIndicators = true;
            config.ShowPriorityIndicators = true;
            config.EnableOverflowIndicators = true;

            _notificationManager.SetGlobalConfiguration(config);
        }

        /// <summary>
        /// Shows an info notification
        /// </summary>
        private void ShowInfoNotificationBtn_Click(object sender, RoutedEventArgs e)
        {
            var data = NotificationData.CreateInfo(
                "Information",
                "This is an informational notification with standard blue styling and info icon."
            );
            _notificationManager.ShowNotification(data);
        }

        /// <summary>
        /// Shows a success notification
        /// </summary>
        private void ShowSuccessNotificationBtn_Click(object sender, RoutedEventArgs e)
        {
            var data = NotificationData.CreateSuccess(
                "Success",
                "Operation completed successfully! This uses green styling and success icon."
            );
            _notificationManager.ShowNotification(data);
        }

        /// <summary>
        /// Shows a warning notification
        /// </summary>
        private void ShowWarningNotificationBtn_Click(object sender, RoutedEventArgs e)
        {
            var data = NotificationData.CreateWarning(
                "Warning",
                "This is a warning notification that requires attention. Uses orange styling."
            );
            _notificationManager.ShowNotification(data);
        }

        /// <summary>
        /// Shows an error notification
        /// </summary>
        private void ShowErrorNotificationBtn_Click(object sender, RoutedEventArgs e)
        {
            var data = NotificationData.CreateError(
                "Error",
                "An error has occurred! This critical notification requires manual dismissal."
            );
            _notificationManager.ShowNotification(data);
        }

        /// <summary>
        /// Shows a system notification
        /// </summary>
        private void ShowSystemNotificationBtn_Click(object sender, RoutedEventArgs e)
        {
            var data = NotificationData.CreateSystem(
                "System Update",
                "System maintenance will begin in 5 minutes. Please save your work."
            );
            _notificationManager.ShowNotification(data);
        }

        /// <summary>
        /// Shows a low priority notification
        /// </summary>
        private void ShowLowPriorityBtn_Click(object sender, RoutedEventArgs e)
        {
            var data = new NotificationData
            {
                Title = "Low Priority",
                Message = "This is a low priority notification that appears at the bottom of the stack.",
                Category = NotificationCategory.Info,
                Priority = NotificationPriority.Low
            };
            _notificationManager.ShowNotification(data);
        }

        /// <summary>
        /// Shows a normal priority notification
        /// </summary>
        private void ShowNormalPriorityBtn_Click(object sender, RoutedEventArgs e)
        {
            var data = new NotificationData
            {
                Title = "Normal Priority",
                Message = "This is a normal priority notification with standard behavior.",
                Category = NotificationCategory.Info,
                Priority = NotificationPriority.Normal
            };
            _notificationManager.ShowNotification(data);
        }

        /// <summary>
        /// Shows a high priority notification
        /// </summary>
        private void ShowHighPriorityBtn_Click(object sender, RoutedEventArgs e)
        {
            var data = new NotificationData
            {
                Title = "High Priority",
                Message = "This is a high priority notification that appears higher in the stack with priority indicator.",
                Category = NotificationCategory.Warning,
                Priority = NotificationPriority.High
            };
            _notificationManager.ShowNotification(data);
        }

        /// <summary>
        /// Shows a critical priority notification
        /// </summary>
        private void ShowCriticalPriorityBtn_Click(object sender, RoutedEventArgs e)
        {
            var data = new NotificationData
            {
                Title = "Critical Priority",
                Message = "This is a critical notification that appears at the top and requires manual dismissal.",
                Category = NotificationCategory.Error,
                Priority = NotificationPriority.Critical
            };
            _notificationManager.ShowNotification(data);
        }

        /// <summary>
        /// Tests compact mode with multiple notifications
        /// </summary>
        private void TestCompactModeBtn_Click(object sender, RoutedEventArgs e)
        {
            // Show enough notifications to trigger compact mode
            for (int i = 1; i <= 5; i++)
            {
                var data = new NotificationData
                {
                    Title = $"Compact Mode Test {i}",
                    Message = $"This is notification {i} of 5. When 3+ notifications are shown, they should automatically use compact mode.",
                    Category = (NotificationCategory)(i % 5),
                    Priority = NotificationPriority.Normal
                };
                _notificationManager.ShowNotification(data);

                // Small delay to demonstrate stacking
                System.Threading.Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Tests overflow handling with many notifications
        /// </summary>
        private void TestOverflowHandlingBtn_Click(object sender, RoutedEventArgs e)
        {
            // Show more notifications than the max visible limit
            for (int i = 1; i <= 10; i++)
            {
                var data = new NotificationData
                {
                    Title = $"Overflow Test {i}",
                    Message = $"This is notification {i} of 10. Notifications beyond the limit should be queued.",
                    Category = (NotificationCategory)(i % 5),
                    Priority = (NotificationPriority)((i % 4) + 1)
                };
                _notificationManager.ShowNotification(data);
            }
        }

        /// <summary>
        /// Tests priority ordering with mixed priority notifications
        /// </summary>
        private void TestPriorityOrderingBtn_Click(object sender, RoutedEventArgs e)
        {
            var notifications = new[]
            {
                ("Low Priority First", NotificationPriority.Low),
                ("Critical Priority", NotificationPriority.Critical),
                ("Normal Priority", NotificationPriority.Normal),
                ("High Priority", NotificationPriority.High),
                ("Another Low", NotificationPriority.Low),
                ("Another Critical", NotificationPriority.Critical)
            };

            foreach (var (title, priority) in notifications)
            {
                var data = new NotificationData
                {
                    Title = title,
                    Message = $"This {priority} notification should be ordered by priority (Critical > High > Normal > Low).",
                    Category = NotificationCategory.Info,
                    Priority = priority
                };
                _notificationManager.ShowNotification(data);

                System.Threading.Thread.Sleep(300);
            }
        }

        /// <summary>
        /// Tests category grouping with different categories
        /// </summary>
        private void TestCategoryGroupingBtn_Click(object sender, RoutedEventArgs e)
        {
            var config = _notificationManager.GetGlobalConfiguration();
            config.EnableCategoryGrouping = true;
            _notificationManager.SetGlobalConfiguration(config);

            var categories = new[]
            {
                NotificationCategory.Error,
                NotificationCategory.Warning,
                NotificationCategory.Info,
                NotificationCategory.Success,
                NotificationCategory.System
            };

            foreach (var category in categories)
            {
                var data = new NotificationData
                {
                    Title = $"{category} Notification",
                    Message = $"This is a {category.ToString().ToLower()} notification with category grouping enabled.",
                    Category = category,
                    Priority = NotificationPriority.Normal
                };
                _notificationManager.ShowNotification(data);

                System.Threading.Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Shows current queue status
        /// </summary>
        private void ShowQueueStatusBtn_Click(object sender, RoutedEventArgs e)
        {
            var status = _notificationManager.GetQueueStatus();
            var activeNotifications = _notificationManager.GetActiveNotifications();
            var pendingNotifications = _notificationManager.GetPendingNotifications();

            var message = $"Active: {status.ActiveCount}/{status.MaxVisible}, " +
                         $"Pending: {status.PendingCount}/{status.MaxQueue}, " +
                         $"Total: {status.TotalCount}, " +
                         $"Overflowing: {(status.IsOverflowing ? "Yes" : "No")}";

            if (activeNotifications.Count > 0)
            {
                message += "\n\nActive by category:";
                var categoryCounts = activeNotifications.GroupBy(n => n.Category)
                    .ToDictionary(g => g.Key, g => g.Count());
                foreach (var (category, count) in categoryCounts)
                {
                    message += $"\n- {category}: {count}";
                }
            }

            var statusData = new NotificationData
            {
                Title = "Queue Status",
                Message = message,
                Category = NotificationCategory.System,
                Priority = NotificationPriority.Normal
            };
            _notificationManager.ShowNotification(statusData);
        }

        /// <summary>
        /// Tests vertical stacking and repositioning animations
        /// </summary>
        private void TestVerticalStackingBtn_Click(object sender, RoutedEventArgs e)
        {
            // Show 4 notifications to test vertical stacking
            for (int i = 1; i <= 4; i++)
            {
                var data = new NotificationData
                {
                    Title = $"Stacking Test {i}",
                    Message = $"This is notification {i}. Dismiss any notification to see repositioning animations.",
                    Category = (NotificationCategory)((i - 1) % 5),
                    Priority = NotificationPriority.Normal,
                    CustomTimeout = TimeSpan.FromSeconds(15) // Longer timeout for testing
                };
                _notificationManager.ShowNotification(data);
                System.Threading.Thread.Sleep(200);
            }

            // Show instructions
            var instructionData = new NotificationData
            {
                Title = "Vertical Stacking Test",
                Message = "4 notifications are now stacked vertically. Try dismissing any notification to see smooth repositioning!",
                Category = NotificationCategory.System,
                Priority = NotificationPriority.High,
                CustomTimeout = TimeSpan.FromSeconds(8)
            };
            _notificationManager.ShowNotification(instructionData);
        }

        /// <summary>
        /// Clears all notifications
        /// </summary>
        private void ClearAllNotificationsBtn_Click(object sender, RoutedEventArgs e)
        {
            _notificationManager.CloseAllNotifications();

            // Show confirmation
            var data = new NotificationData
            {
                Title = "Cleared",
                Message = "All notifications have been cleared from the queue and display.",
                Category = NotificationCategory.Success,
                Priority = NotificationPriority.Normal
            };
            _notificationManager.ShowNotification(data);
        }

        /// <summary>
        /// Tests the global hotkey system by showing registered hotkeys
        /// </summary>
        private void TestHotkeySystemBtn_Click(object sender, RoutedEventArgs e)
        {
            var registeredHotkeys = _hotkeyManager.GetRegisteredHotkeys();
            var systemConfig = _hotkeyManager.SystemConfiguration;

            var message = $"Hotkey System Status:\n" +
                         $"Enabled: {_hotkeyManager.IsEnabled}\n" +
                         $"Registered Hotkeys: {registeredHotkeys.Count}\n" +
                         $"Window Handle Available: {_hotkeyManager.IsWindowHandleAvailable}\n\n";

            if (registeredHotkeys.Count > 0)
            {
                message += "Registered Hotkeys:\n";
                foreach (var hotkey in registeredHotkeys)
                {
                    message += $"• {hotkey.DisplayName}: {hotkey.Description}\n";
                }
            }
            else
            {
                message += "No hotkeys currently registered.\n";
                message += "Default hotkeys should be:\n";
                message += "• Ctrl+Shift+D: Dismiss All Notifications\n";
                message += "• Ctrl+Shift+Esc: Dismiss Latest Notification\n";
                message += "• Ctrl+Shift+P: Toggle Notification System\n";
                message += "• Ctrl+Shift+Q: Show Queue Status\n";
            }

            var data = new NotificationData
            {
                Title = "Hotkey System Test",
                Message = message,
                Category = NotificationCategory.System,
                Priority = NotificationPriority.Normal,
                CustomTimeout = TimeSpan.FromSeconds(10)
            };
            _notificationManager.ShowNotification(data);
        }

        #endregion

        #region Hotkey Configuration Event Handlers

        /// <summary>
        /// Handles enable/disable hotkey system checkbox changes
        /// </summary>
        private void EnableHotkeysCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                // Skip if managers are not initialized yet (during XAML loading)
                if (_hotkeyManager == null || _notificationManager == null)
                    return;

                var isEnabled = EnableHotkeysCheckBox.IsChecked == true;
                _hotkeyManager.IsEnabled = isEnabled;

                // Update UI status
                UpdateHotkeySystemStatus();
                RefreshHotkeyStatus();

                var data = NotificationData.CreateInfo(
                    "Hotkey System",
                    $"Global hotkeys are now {(isEnabled ? "enabled" : "disabled")}"
                );
                _notificationManager.ShowNotification(data);
            }
            catch (Exception ex)
            {
                ShowHotkeyError($"Failed to toggle hotkey system: {ex.Message}");
            }
        }

        /// <summary>
        /// Resets all hotkeys to their default configurations
        /// </summary>
        private void ResetHotkeysBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "This will reset all hotkeys to their default configurations. Are you sure?",
                    "Reset Hotkeys",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Unregister all current hotkeys
                    _hotkeyManager.UnregisterAllHotkeys();

                    // Register default hotkeys
                    _hotkeyManager.RegisterDefaultHotkeys();

                    // Update UI to show defaults
                    UpdateHotkeyDisplays();
                    RefreshHotkeyStatus();

                    var data = NotificationData.CreateSuccess(
                        "Hotkeys Reset",
                        "All hotkeys have been reset to their default configurations."
                    );
                    _notificationManager.ShowNotification(data);
                }
            }
            catch (Exception ex)
            {
                ShowHotkeyError($"Failed to reset hotkeys: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens the edit dialog for the DismissAll hotkey
        /// </summary>
        private void EditDismissAllBtn_Click(object sender, RoutedEventArgs e)
        {
            EditHotkey("DismissAll", "Dismiss All Notifications", ModifierKeys.Control | ModifierKeys.Shift, Key.D);
        }

        /// <summary>
        /// Opens the edit dialog for the DismissLatest hotkey
        /// </summary>
        private void EditDismissLatestBtn_Click(object sender, RoutedEventArgs e)
        {
            EditHotkey("DismissLatest", "Dismiss Latest Notification", ModifierKeys.Control | ModifierKeys.Shift, Key.Escape);
        }

        /// <summary>
        /// Opens the edit dialog for the ToggleSystem hotkey
        /// </summary>
        private void EditToggleSystemBtn_Click(object sender, RoutedEventArgs e)
        {
            EditHotkey("ToggleSystem", "Toggle Notification System", ModifierKeys.Control | ModifierKeys.Shift, Key.P);
        }

        /// <summary>
        /// Opens the edit dialog for the ShowQueueStatus hotkey
        /// </summary>
        private void EditShowQueueStatusBtn_Click(object sender, RoutedEventArgs e)
        {
            EditHotkey("ShowQueueStatus", "Show Queue Status", ModifierKeys.Control | ModifierKeys.Shift, Key.Q);
        }

        /// <summary>
        /// Handles clicking on hotkey display areas to edit them
        /// </summary>
        private void DismissAllHotkey_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => EditDismissAllBtn_Click(sender, e);
        private void DismissLatestHotkey_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => EditDismissLatestBtn_Click(sender, e);
        private void ToggleSystemHotkey_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => EditToggleSystemBtn_Click(sender, e);
        private void ShowQueueStatusHotkey_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => EditShowQueueStatusBtn_Click(sender, e);

        /// <summary>
        /// Tests the DismissAll hotkey functionality
        /// </summary>
        private void TestDismissAllBtn_Click(object sender, RoutedEventArgs e)
        {
            TestHotkeyAction("DismissAll", () => {
                _notificationManager.CloseAllNotifications();
                return "All notifications dismissed";
            });
        }

        /// <summary>
        /// Tests the DismissLatest hotkey functionality
        /// </summary>
        private void TestDismissLatestBtn_Click(object sender, RoutedEventArgs e)
        {
            TestHotkeyAction("DismissLatest", () => {
                var activeNotifications = _notificationManager.GetActiveNotifications();
                if (activeNotifications.Count > 0)
                {
                    var latest = activeNotifications.OrderByDescending(n => n.CreatedAt).First();
                    _notificationManager.CloseNotification(latest.Id);
                    return $"Dismissed latest notification: {latest.Title}";
                }
                return "No active notifications to dismiss";
            });
        }

        /// <summary>
        /// Tests the ToggleSystem hotkey functionality
        /// </summary>
        private void TestToggleSystemBtn_Click(object sender, RoutedEventArgs e)
        {
            TestHotkeyAction("ToggleSystem", () => {
                var status = _notificationManager.GetQueueStatus();
                return $"System Status - Active: {status.ActiveCount}, Pending: {status.PendingCount}";
            });
        }

        /// <summary>
        /// Tests the ShowQueueStatus hotkey functionality
        /// </summary>
        private void TestShowQueueStatusBtn_Click(object sender, RoutedEventArgs e)
        {
            TestHotkeyAction("ShowQueueStatus", () => {
                ShowQueueStatusBtn_Click(sender, e);
                return "Queue status notification shown";
            });
        }

        /// <summary>
        /// Tests all hotkey functionality
        /// </summary>
        private void TestAllHotkeysBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var registeredHotkeys = _hotkeyManager.GetRegisteredHotkeys();
                var message = $"Testing {registeredHotkeys.Count} registered hotkeys:\n\n";

                foreach (var hotkey in registeredHotkeys)
                {
                    var status = hotkey.IsRegistered ? "✓ Registered" : "✗ Not Registered";
                    message += $"• {hotkey.DisplayName}: {hotkey.Description} - {status}\n";
                }

                var data = new NotificationData
                {
                    Title = "Hotkey System Test",
                    Message = message,
                    Category = NotificationCategory.System,
                    Priority = NotificationPriority.Normal,
                    CustomTimeout = TimeSpan.FromSeconds(10)
                };
                _notificationManager.ShowNotification(data);

                UpdateHotkeyConfigStatus($"All hotkeys tested. {registeredHotkeys.Count} hotkeys are currently registered.");
            }
            catch (Exception ex)
            {
                ShowHotkeyError($"Failed to test hotkeys: {ex.Message}");
            }
        }

        /// <summary>
        /// Refreshes the hotkey status display
        /// </summary>
        private void RefreshHotkeyStatusBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshHotkeyStatus();
            UpdateHotkeyDisplays();

            var data = NotificationData.CreateInfo(
                "Hotkey Status Refreshed",
                "Hotkey configuration status has been updated."
            );
            _notificationManager.ShowNotification(data);
        }

        #endregion

        #region Hotkey Configuration Helper Methods

        /// <summary>
        /// Generic method to edit a hotkey configuration
        /// </summary>
        private void EditHotkey(string actionName, string description, ModifierKeys defaultModifiers, Key defaultKey)
        {
            try
            {
                // Find existing hotkey or create default
                var registeredHotkeys = _hotkeyManager.GetRegisteredHotkeys();
                var existingHotkey = registeredHotkeys.FirstOrDefault(h => h.Description.Contains(description.Split(' ')[0]));

                var currentHotkey = existingHotkey ?? new HotkeyConfiguration
                {
                    ModifierKeys = defaultModifiers,
                    Key = defaultKey,
                    Description = description
                };

                // Open edit dialog
                var editWindow = new HotkeyEditWindow(currentHotkey, description)
                {
                    Owner = this
                };

                if (editWindow.ShowDialog() == true && editWindow.EditedHotkey != null)
                {
                    var newHotkey = editWindow.EditedHotkey;

                    // Unregister old hotkey if it exists
                    if (existingHotkey != null)
                    {
                        _hotkeyManager.UnregisterHotkey(existingHotkey);
                    }

                    // Set up the action for the new hotkey
                    newHotkey.Action = GetHotkeyAction(actionName);

                    // Register new hotkey
                    var success = _hotkeyManager.RegisterHotkey(newHotkey);

                    if (success)
                    {
                        UpdateHotkeyDisplays();
                        RefreshHotkeyStatus();

                        var data = NotificationData.CreateSuccess(
                            "Hotkey Updated",
                            $"{description} hotkey changed to: {newHotkey.DisplayName}"
                        );
                        _notificationManager.ShowNotification(data);
                    }
                    else
                    {
                        // Try to re-register the old hotkey if new one failed
                        if (existingHotkey != null)
                        {
                            _hotkeyManager.RegisterHotkey(existingHotkey);
                        }

                        ShowHotkeyError($"Failed to register new hotkey: {newHotkey.DisplayName}");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowHotkeyError($"Error editing hotkey: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the appropriate action for a hotkey based on its name
        /// </summary>
        private Action GetHotkeyAction(string actionName)
        {
            return actionName switch
            {
                "DismissAll" => () => _notificationManager.CloseAllNotifications(),
                "DismissLatest" => () => {
                    var activeNotifications = _notificationManager.GetActiveNotifications();
                    if (activeNotifications.Count > 0)
                    {
                        var latest = activeNotifications.OrderByDescending(n => n.CreatedAt).First();
                        _notificationManager.CloseNotification(latest.Id);
                    }
                },
                "ToggleSystem" => () => {
                    var status = _notificationManager.GetQueueStatus();
                    var message = $"Active: {status.ActiveCount}, Pending: {status.PendingCount}";
                    var data = NotificationData.CreateSystem("System Status", message);
                    _notificationManager.ShowNotification(data);
                },
                "ShowQueueStatus" => () => ShowQueueStatusBtn_Click(null!, new RoutedEventArgs()),
                _ => () => { /* No action */ }
            };
        }

        /// <summary>
        /// Tests a hotkey action and shows the result
        /// </summary>
        private void TestHotkeyAction(string actionName, Func<string> testAction)
        {
            try
            {
                var result = testAction();

                var data = NotificationData.CreateInfo(
                    $"Hotkey Test: {actionName}",
                    result
                );
                _notificationManager.ShowNotification(data);
            }
            catch (Exception ex)
            {
                ShowHotkeyError($"Test failed for {actionName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates all hotkey displays with current configurations
        /// </summary>
        private void UpdateHotkeyDisplays()
        {
            try
            {
                var registeredHotkeys = _hotkeyManager.GetRegisteredHotkeys();

                // Update DismissAll hotkey display
                var dismissAllHotkey = registeredHotkeys.FirstOrDefault(h => h.Description.Contains("Dismiss All"));
                DismissAllHotkeyText.Text = dismissAllHotkey?.DisplayName ?? "Ctrl + Shift + D";

                // Update DismissLatest hotkey display
                var dismissLatestHotkey = registeredHotkeys.FirstOrDefault(h => h.Description.Contains("Dismiss Latest"));
                DismissLatestHotkeyText.Text = dismissLatestHotkey?.DisplayName ?? "Ctrl + Shift + Esc";

                // Update ToggleSystem hotkey display
                var toggleSystemHotkey = registeredHotkeys.FirstOrDefault(h => h.Description.Contains("Toggle"));
                ToggleSystemHotkeyText.Text = toggleSystemHotkey?.DisplayName ?? "Ctrl + Shift + P";

                // Update ShowQueueStatus hotkey display
                var showQueueStatusHotkey = registeredHotkeys.FirstOrDefault(h => h.Description.Contains("Queue Status"));
                ShowQueueStatusHotkeyText.Text = showQueueStatusHotkey?.DisplayName ?? "Ctrl + Shift + Q";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating hotkey displays: {ex.Message}");
            }
        }

        /// <summary>
        /// Refreshes the hotkey status indicators and system status
        /// </summary>
        private void RefreshHotkeyStatus()
        {
            try
            {
                var registeredHotkeys = _hotkeyManager.GetRegisteredHotkeys();
                var systemEnabled = _hotkeyManager.IsEnabled;

                // Update system status
                UpdateHotkeySystemStatus();

                // Update individual hotkey status indicators
                UpdateHotkeyStatusIndicator(DismissAllStatusIndicator,
                    registeredHotkeys.Any(h => h.Description.Contains("Dismiss All") && h.IsRegistered));

                UpdateHotkeyStatusIndicator(DismissLatestStatusIndicator,
                    registeredHotkeys.Any(h => h.Description.Contains("Dismiss Latest") && h.IsRegistered));

                UpdateHotkeyStatusIndicator(ToggleSystemStatusIndicator,
                    registeredHotkeys.Any(h => h.Description.Contains("Toggle") && h.IsRegistered));

                UpdateHotkeyStatusIndicator(ShowQueueStatusStatusIndicator,
                    registeredHotkeys.Any(h => h.Description.Contains("Queue Status") && h.IsRegistered));

                // Update overall status message
                var registeredCount = registeredHotkeys.Count(h => h.IsRegistered);
                var totalCount = registeredHotkeys.Count;

                string statusMessage;
                if (!systemEnabled)
                {
                    statusMessage = "Hotkey system is disabled. Enable it to use global hotkeys.";
                }
                else if (registeredCount == totalCount && totalCount > 0)
                {
                    statusMessage = $"All {totalCount} hotkeys registered successfully. Click 'Edit' to change key combinations or use 'Test' to verify functionality.";
                }
                else if (registeredCount > 0)
                {
                    statusMessage = $"{registeredCount} of {totalCount} hotkeys registered successfully. Some hotkeys may have conflicts.";
                }
                else
                {
                    statusMessage = "No hotkeys are currently registered. Click 'Reset to Defaults' to restore default hotkeys.";
                }

                UpdateHotkeyConfigStatus(statusMessage);
            }
            catch (Exception ex)
            {
                UpdateHotkeyConfigStatus($"Error refreshing hotkey status: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the hotkey system status display
        /// </summary>
        private void UpdateHotkeySystemStatus()
        {
            var isEnabled = _hotkeyManager.IsEnabled;
            EnableHotkeysCheckBox.IsChecked = isEnabled;

            if (isEnabled)
            {
                HotkeySystemStatusText.Text = "System Active";
                HotkeySystemStatusText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27AE60")!);
            }
            else
            {
                HotkeySystemStatusText.Text = "System Disabled";
                HotkeySystemStatusText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E74C3C")!);
            }
        }

        /// <summary>
        /// Updates a hotkey status indicator
        /// </summary>
        private void UpdateHotkeyStatusIndicator(System.Windows.Shapes.Ellipse indicator, bool isRegistered)
        {
            if (isRegistered)
            {
                indicator.Fill = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27AE60")!);
                indicator.ToolTip = "Hotkey registered successfully";
            }
            else
            {
                indicator.Fill = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E74C3C")!);
                indicator.ToolTip = "Hotkey registration failed or disabled";
            }
        }

        /// <summary>
        /// Updates the hotkey configuration status text
        /// </summary>
        private void UpdateHotkeyConfigStatus(string message)
        {
            HotkeyConfigStatusText.Text = message;
        }

        /// <summary>
        /// Shows a hotkey-related error notification
        /// </summary>
        private void ShowHotkeyError(string message)
        {
            // Check if notification manager is initialized (avoid issues during XAML loading)
            if (_notificationManager != null)
            {
                var data = NotificationData.CreateError("Hotkey Error", message);
                _notificationManager.ShowNotification(data);
            }
            else
            {
                // Fallback to debug output if notification manager not available
                System.Diagnostics.Debug.WriteLine($"Hotkey Error: {message}");
            }
        }

        /// <summary>
        /// Initializes the hotkey UI when the window loads
        /// </summary>
        private void InitializeHotkeyUI()
        {
            // This method is called from MainWindow_Loaded
            UpdateHotkeyDisplays();
            RefreshHotkeyStatus();
        }

        /// <summary>
        /// Runs comprehensive validation tests for the hotkey system
        /// </summary>
        private async void RunHotkeyValidationTests()
        {
            try
            {
                var report = await HotkeyValidationTest.RunValidationAsync();

                var data = NotificationData.CreateSystem(
                    "Hotkey Validation Complete",
                    $"Tests: {report.TestsRun}, Passed: {report.TestsPassed}, Failed: {report.TestsFailed}\n" +
                    $"Success Rate: {report.SuccessRate:F1}%"
                );
                _notificationManager?.ShowNotification(data);

                // Output detailed results to debug console
                System.Diagnostics.Debug.WriteLine("\n=== HOTKEY VALIDATION REPORT ===");
                System.Diagnostics.Debug.WriteLine(report.ToString());
                System.Diagnostics.Debug.WriteLine("================================\n");
            }
            catch (Exception ex)
            {
                ShowHotkeyError($"Validation test failed: {ex.Message}");
            }
        }

        #endregion
    }
}