using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace IntervalToast
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// A custom notification window that appears as a toast notification with proper positioning and behavior.
    /// </summary>
    public partial class NotificationWindow : Window
    {
        #region Win32 API Declarations

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static readonly IntPtr HWND_NOACTIVATE = new IntPtr(-4);
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const int SW_SHOWNOACTIVATE = 4;

        #endregion

        #region Fields and Properties

        private DispatcherTimer? _autoCloseTimer;
        private readonly NotificationConfiguration _config;
        private bool _isClosing;
        private bool _isAnimating;
        private Storyboard? _progressStoryboard;
        private bool _isHovered;
        private NotificationData? _notificationData;
        private bool _isCompactMode;

        /// <summary>
        /// Gets or sets the notification title
        /// </summary>
        public string NotificationTitle
        {
            get => TitleTextBlock.Text;
            set => TitleTextBlock.Text = value;
        }

        /// <summary>
        /// Gets or sets the notification message
        /// </summary>
        public string NotificationMessage
        {
            get => MessageTextBlock.Text;
            set => MessageTextBlock.Text = value;
        }

        /// <summary>
        /// Gets or sets the timestamp text
        /// </summary>
        public string Timestamp
        {
            get => TimestampTextBlock.Text;
            set => TimestampTextBlock.Text = value;
        }

        /// <summary>
        /// Event raised when the notification is closed
        /// </summary>
        public event EventHandler? NotificationClosed;

        /// <summary>
        /// Event raised when entry animation completes
        /// </summary>
        public event EventHandler? EntryAnimationCompleted;

        /// <summary>
        /// Event raised when exit animation completes
        /// </summary>
        public event EventHandler? ExitAnimationCompleted;

        /// <summary>
        /// Gets or sets the notification data
        /// </summary>
        public NotificationData? NotificationData
        {
            get => _notificationData;
            set
            {
                _notificationData = value;
                if (value != null)
                {
                    ApplyCategoryAndPriorityVisuals(value);
                }
            }
        }

        /// <summary>
        /// Gets whether this notification is in compact mode
        /// </summary>
        public bool IsCompactMode => _isCompactMode;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the NotificationWindow class
        /// </summary>
        /// <param name="config">Configuration settings for the notification</param>
        public NotificationWindow(NotificationConfiguration? config = null)
        {
            InitializeComponent();

            _config = config ?? new NotificationConfiguration();
            _isClosing = false;

            // Apply configuration
            ApplyConfiguration();

            // Set initial timestamp
            Timestamp = DateTime.Now.ToString("HH:mm");

            // Configure window behavior
            ConfigureWindowBehavior();

            // Apply animation preset
            _config.ApplyAnimationPreset(_config.AnimationPreset);

            // Setup progress indicator
            SetupProgressIndicator();
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Applies the notification configuration to the window
        /// </summary>
        private void ApplyConfiguration()
        {
            // Apply size
            Width = _config.Width;
            Height = _config.Height;

            // Apply colors
            var backgroundBrush = new SolidColorBrush(_config.BackgroundColor);
            NotificationBorder.Background = backgroundBrush;

            var titleBrush = new SolidColorBrush(_config.TitleColor);
            TitleTextBlock.Foreground = titleBrush;

            var messageBrush = new SolidColorBrush(_config.MessageColor);
            MessageTextBlock.Foreground = messageBrush;

            // Apply fonts
            TitleTextBlock.FontFamily = new System.Windows.Media.FontFamily(_config.TitleFontFamily);
            TitleTextBlock.FontSize = _config.TitleFontSize;

            MessageTextBlock.FontFamily = new System.Windows.Media.FontFamily(_config.MessageFontFamily);
            MessageTextBlock.FontSize = _config.MessageFontSize;

            // Apply corner radius
            NotificationBorder.CornerRadius = new CornerRadius(_config.CornerRadius);

            // Apply opacity
            NotificationBorder.Opacity = _config.Opacity;

            // Apply visual effects
            ApplyVisualEffects();

            // Configure close button visibility
            CloseButton.Visibility = _config.ShowCloseButton ? Visibility.Visible : Visibility.Collapsed;

            // Configure timestamp visibility
            TimestampTextBlock.Visibility = _config.ShowTimestamp ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Window Behavior

        /// <summary>
        /// Configures the window to behave as a proper notification toast
        /// </summary>
        private void ConfigureWindowBehavior()
        {
            // Handle loaded event to ensure proper positioning and behavior
            Loaded += NotificationWindow_Loaded;

            // Handle source initialization for Win32 integration
            SourceInitialized += NotificationWindow_SourceInitialized;

            // Start auto-close timer if configured
            if (_config.AutoCloseDelay > TimeSpan.Zero)
            {
                StartAutoCloseTimer();
            }
        }

        /// <summary>
        /// Handles the window loaded event
        /// </summary>
        private void NotificationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Position the window using the position manager
            var positionManager = new WindowPositionManager();
            var position = positionManager.CalculatePosition(this, _config.Position);

            Left = position.X;
            Top = position.Y;

            // Start entry animation
            StartEntryAnimation();
        }

        /// <summary>
        /// Handles the source initialization event for Win32 integration
        /// </summary>
        private void NotificationWindow_SourceInitialized(object? sender, EventArgs e)
        {
            // Ensure the window doesn't steal focus
            var hwnd = new WindowInteropHelper(this).Handle;

            // Show the window without activation
            ShowWindow(hwnd, SW_SHOWNOACTIVATE);

            // Set window position as topmost without activation
            SetWindowPos(hwnd, HWND_NOACTIVATE, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        #endregion

        #region Auto-Close Timer

        /// <summary>
        /// Starts the auto-close timer
        /// </summary>
        private void StartAutoCloseTimer()
        {
            _autoCloseTimer = new DispatcherTimer
            {
                Interval = _config.AutoCloseDelay
            };

            _autoCloseTimer.Tick += AutoCloseTimer_Tick;
            _autoCloseTimer.Start();
        }

        /// <summary>
        /// Handles the auto-close timer tick event
        /// </summary>
        private void AutoCloseTimer_Tick(object? sender, EventArgs e)
        {
            CloseNotification();
        }

        /// <summary>
        /// Stops the auto-close timer
        /// </summary>
        private void StopAutoCloseTimer()
        {
            if (_autoCloseTimer != null)
            {
                _autoCloseTimer.Stop();
                _autoCloseTimer.Tick -= AutoCloseTimer_Tick;
                _autoCloseTimer = null;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the close button click event
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseNotification();
        }

        /// <summary>
        /// Handles mouse enter event to pause auto-close and trigger hover effects
        /// </summary>
        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            _isHovered = true;

            // Pause auto-close timer when mouse is over the notification
            if (_config.PauseOnHover)
            {
                _autoCloseTimer?.Stop();
                _progressStoryboard?.Pause();
            }

            // Trigger hover animation
            AnimationEngine.AnimateHover(this, _config, true);
        }

        /// <summary>
        /// Handles mouse leave event to resume auto-close and end hover effects
        /// </summary>
        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            _isHovered = false;

            // Resume auto-close timer when mouse leaves the notification
            if (_config.AutoCloseDelay > TimeSpan.Zero && !_isClosing && _config.PauseOnHover)
            {
                _autoCloseTimer?.Start();
                _progressStoryboard?.Resume();
            }

            // Trigger hover animation end
            AnimationEngine.AnimateHover(this, _config, false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the notification with the specified content
        /// </summary>
        /// <param name="title">The notification title</param>
        /// <param name="message">The notification message</param>
        public void ShowNotification(string title, string message)
        {
            NotificationTitle = title;
            NotificationMessage = message;
            Timestamp = DateTime.Now.ToString("HH:mm");

            Show();
        }

        /// <summary>
        /// Updates the position of this notification window (repositioning disabled for simplified system)
        /// </summary>
        /// <param name="newPosition">The new position to animate to</param>
        /// <param name="onCompleted">Optional callback when animation completes</param>
        public void AnimateToPosition(System.Windows.Point newPosition, EventHandler? onCompleted = null)
        {
            // Repositioning disabled in simplified system - no action taken
            onCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Closes the notification window properly with exit animation
        /// </summary>
        public void CloseNotification()
        {
            if (_isClosing) return;

            _isClosing = true;
            StopAutoCloseTimer();
            StopProgressAnimation();

            // Start exit animation
            AnimationEngine.AnimateExit(this, _config, (sender, e) =>
            {
                // Raise the exit animation completed event
                ExitAnimationCompleted?.Invoke(this, EventArgs.Empty);

                // Raise the closed event
                NotificationClosed?.Invoke(this, EventArgs.Empty);

                // Close the window
                Close();
            });
        }

        #endregion

        #region Animation Methods

        /// <summary>
        /// Starts the entry animation for the notification
        /// </summary>
        private void StartEntryAnimation()
        {
            if (_isAnimating) return;

            _isAnimating = true;

            AnimationEngine.AnimateEntry(this, _config, (sender, e) =>
            {
                _isAnimating = false;
                EntryAnimationCompleted?.Invoke(this, EventArgs.Empty);

                // Start progress animation after entry completes
                if (_config.ShowProgressIndicator && _config.AutoCloseDelay > TimeSpan.Zero)
                {
                    StartProgressAnimation();
                }
            });
        }

        /// <summary>
        /// Applies visual effects based on configuration
        /// </summary>
        private void ApplyVisualEffects()
        {
            try
            {
                if (!_config.EnableVisualEffects) return;

                // Apply blur effect if configured
                if (_config.BlurRadius > 0)
                {
                    try
                    {
                        var blurEffect = (BlurEffect)FindResource("NotificationBlur");
                        if (blurEffect != null)
                        {
                            blurEffect.Radius = _config.BlurRadius;
                            NotificationBorder.Effect = blurEffect;
                        }
                    }
                    catch
                    {
                        // Create a new blur effect if resource not found
                        var blurEffect = new BlurEffect
                        {
                            Radius = _config.BlurRadius
                        };
                        NotificationBorder.Effect = blurEffect;
                    }
                }

                // Apply glow effect if configured
                if (_config.GlowIntensity > 0)
                {
                    try
                    {
                        var glowEffect = (DropShadowEffect)FindResource("NotificationGlow");
                        if (glowEffect != null)
                        {
                            glowEffect.Opacity = _config.GlowIntensity;

                            // Combine with existing shadow if both are enabled
                            if (_config.BlurRadius <= 0)
                            {
                                NotificationBorder.Effect = glowEffect;
                            }
                        }
                    }
                    catch
                    {
                        // Create a new glow effect if resource not found
                        var glowEffect = new DropShadowEffect
                        {
                            Color = System.Windows.Media.Color.FromRgb(74, 144, 226),
                            Opacity = _config.GlowIntensity,
                            BlurRadius = 20,
                            ShadowDepth = 0
                        };

                        if (_config.BlurRadius <= 0)
                        {
                            NotificationBorder.Effect = glowEffect;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't crash
                System.Diagnostics.Debug.WriteLine($"ApplyVisualEffects error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets up the progress indicator based on configuration
        /// </summary>
        private void SetupProgressIndicator()
        {
            if (!_config.ShowProgressIndicator) return;

            // Hide all progress indicators first
            BottomProgressBar.Visibility = Visibility.Collapsed;
            TopProgressBar.Visibility = Visibility.Collapsed;
            LeftProgressBorder.Visibility = Visibility.Collapsed;
            RightProgressBorder.Visibility = Visibility.Collapsed;
            CircularProgressCorner.Visibility = Visibility.Collapsed;
            CircularProgressCenter.Visibility = Visibility.Collapsed;

            // Show the appropriate progress indicator
            switch (_config.ProgressStyle)
            {
                case ProgressIndicatorStyle.BottomBar:
                    BottomProgressBar.Visibility = Visibility.Visible;
                    break;
                case ProgressIndicatorStyle.TopBar:
                    TopProgressBar.Visibility = Visibility.Visible;
                    break;
                case ProgressIndicatorStyle.LeftBorder:
                    LeftProgressBorder.Visibility = Visibility.Visible;
                    break;
                case ProgressIndicatorStyle.RightBorder:
                    RightProgressBorder.Visibility = Visibility.Visible;
                    break;
                case ProgressIndicatorStyle.CircularCorner:
                    CircularProgressCorner.Visibility = Visibility.Visible;
                    break;
                case ProgressIndicatorStyle.CircularCenter:
                    CircularProgressCenter.Visibility = Visibility.Visible;
                    break;
            }
        }

        /// <summary>
        /// Starts the progress animation for auto-dismiss countdown
        /// </summary>
        private void StartProgressAnimation()
        {
            try
            {
                if (_config.AutoCloseDelay <= TimeSpan.Zero || !_config.ShowProgressIndicator) return;

                var progressElement = GetProgressElement();
                if (progressElement == null) return;

                _progressStoryboard = AnimationEngine.CreateProgressAnimation(
                    progressElement,
                    _config.AutoCloseDelay,
                    _config.ProgressStyle,
                    (sender, e) =>
                    {
                        try
                        {
                            // Progress completed - auto-close the notification
                            if (!_isClosing && !_isHovered)
                            {
                                CloseNotification();
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Progress completion error: {ex.Message}");
                        }
                    });

                if (_progressStoryboard != null)
                {
                    _progressStoryboard.Begin();
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't crash
                System.Diagnostics.Debug.WriteLine($"StartProgressAnimation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops the progress animation
        /// </summary>
        private void StopProgressAnimation()
        {
            try
            {
                if (_progressStoryboard != null)
                {
                    _progressStoryboard.Stop();
                    _progressStoryboard = null;
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't crash
                System.Diagnostics.Debug.WriteLine($"StopProgressAnimation error: {ex.Message}");
                _progressStoryboard = null;
            }
        }

        /// <summary>
        /// Gets the appropriate progress element based on the configured style
        /// </summary>
        private FrameworkElement? GetProgressElement()
        {
            try
            {
                return _config.ProgressStyle switch
                {
                    ProgressIndicatorStyle.BottomBar => BottomProgressBar,
                    ProgressIndicatorStyle.TopBar => TopProgressBar,
                    ProgressIndicatorStyle.LeftBorder => LeftProgressBorder,
                    ProgressIndicatorStyle.RightBorder => RightProgressBorder,
                    ProgressIndicatorStyle.CircularCorner => CircularProgressCorner,
                    ProgressIndicatorStyle.CircularCenter => CircularProgressCenter,
                    _ => null
                };
            }
            catch (Exception ex)
            {
                // Log the error and return null
                System.Diagnostics.Debug.WriteLine($"GetProgressElement error: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Phase 3: Category and Priority Visual Methods

        /// <summary>
        /// Applies category and priority visual indicators to the notification
        /// </summary>
        /// <param name="data">The notification data containing category and priority information</param>
        private void ApplyCategoryAndPriorityVisuals(NotificationData data)
        {
            try
            {
                // Apply category visuals
                ApplyCategoryVisuals(data.Category);

                // Apply priority visuals
                ApplyPriorityVisuals(data.Priority);

                // Set custom icon if provided
                if (!string.IsNullOrEmpty(data.IconContent))
                {
                    CategoryIconText.Text = data.IconContent;
                }

                // Apply compact mode if enabled
                ApplyCompactModeIfNeeded();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying category/priority visuals: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies category-specific visual styling
        /// </summary>
        /// <param name="category">The notification category</param>
        private void ApplyCategoryVisuals(NotificationCategory category)
        {
            try
            {
                if (!_config.ShowCategoryIndicators) return;

                // Get category configuration
                if (_config.CategoryConfigurations.TryGetValue(category, out var categoryConfig))
                {
                    // Apply category colors
                    var backgroundBrush = new SolidColorBrush(categoryConfig.BackgroundColor);
                    var accentBrush = new SolidColorBrush(categoryConfig.AccentColor);

                    NotificationBorder.Background = backgroundBrush;
                    IconBorder.Background = accentBrush;

                    // Show category border if configured
                    if (_config.EnableCategoryGrouping)
                    {
                        CategoryBorder.BorderBrush = accentBrush;
                        CategoryBorder.Visibility = Visibility.Visible;
                    }

                    // Set category icon
                    if (!string.IsNullOrEmpty(categoryConfig.IconContent))
                    {
                        CategoryIconText.Text = categoryConfig.IconContent;
                    }

                    // Apply progress indicator colors
                    ApplyProgressIndicatorColors(accentBrush);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying category visuals: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies priority-specific visual styling
        /// </summary>
        /// <param name="priority">The notification priority</param>
        private void ApplyPriorityVisuals(NotificationPriority priority)
        {
            try
            {
                if (!_config.ShowPriorityIndicators) return;

                // Get priority configuration
                if (_config.PriorityConfigurations.TryGetValue(priority, out var priorityConfig))
                {
                    // Show priority indicator badge for high/critical priorities
                    if (priorityConfig.ShowPriorityIndicator)
                    {
                        PriorityIndicator.Visibility = Visibility.Visible;

                        // Set priority indicator color
                        var priorityBrush = GetPriorityIndicatorBrush(priority);
                        PriorityIndicator.Background = priorityBrush;
                    }
                    else
                    {
                        PriorityIndicator.Visibility = Visibility.Collapsed;
                    }

                    // Apply visual emphasis scaling
                    if (priorityConfig.VisualEmphasis != 1.0)
                    {
                        var scaleTransform = new ScaleTransform(priorityConfig.VisualEmphasis, priorityConfig.VisualEmphasis);
                        if (RenderTransform is TransformGroup group)
                        {
                            group.Children.Add(scaleTransform);
                        }
                        else
                        {
                            RenderTransform = scaleTransform;
                        }
                        RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying priority visuals: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the appropriate brush for priority indicator based on priority level
        /// </summary>
        /// <param name="priority">The notification priority</param>
        /// <returns>The brush for the priority indicator</returns>
        private SolidColorBrush GetPriorityIndicatorBrush(NotificationPriority priority)
        {
            return priority switch
            {
                NotificationPriority.Critical => new SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)), // Red
                NotificationPriority.High => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7)), // Orange
                NotificationPriority.Normal => new SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 144, 226)), // Blue
                NotificationPriority.Low => new SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158)), // Gray
                _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 144, 226))
            };
        }

        /// <summary>
        /// Applies category accent colors to progress indicators
        /// </summary>
        /// <param name="accentBrush">The accent color brush</param>
        private void ApplyProgressIndicatorColors(SolidColorBrush accentBrush)
        {
            try
            {
                BottomProgressBar.Fill = accentBrush;
                TopProgressBar.Fill = accentBrush;
                LeftProgressBorder.Fill = accentBrush;
                RightProgressBorder.Fill = accentBrush;
                CircularProgressCorner.Stroke = accentBrush;
                CircularProgressCenter.Stroke = accentBrush;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying progress indicator colors: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies compact mode styling if needed
        /// </summary>
        private void ApplyCompactModeIfNeeded()
        {
            try
            {
                var notificationCount = _config.MaxVisibleNotifications; // This would ideally come from the manager
                _isCompactMode = _config.EnableCompactMode && notificationCount >= _config.CompactModeThreshold;

                if (_isCompactMode)
                {
                    // Apply compact mode styling
                    var scale = _config.CompactModeScale;

                    // Reduce font sizes
                    TitleTextBlock.FontSize *= scale;
                    MessageTextBlock.FontSize *= scale;
                    TimestampTextBlock.FontSize *= scale;

                    // Reduce icon size
                    IconBorder.Width *= scale;
                    IconBorder.Height *= scale;
                    CategoryIconText.FontSize *= scale;

                    // Reduce spacing
                    MainContentGrid.Margin = new Thickness(
                        MainContentGrid.Margin.Left * scale,
                        MainContentGrid.Margin.Top * scale,
                        MainContentGrid.Margin.Right * scale,
                        MainContentGrid.Margin.Bottom * scale);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying compact mode: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows overflow indicators for queue management
        /// </summary>
        /// <param name="overflowCount">Number of notifications in overflow</param>
        /// <param name="scrollPosition">The scroll indicator position</param>
        public void ShowOverflowIndicators(int overflowCount, ScrollIndicatorPosition scrollPosition = ScrollIndicatorPosition.Right)
        {
            try
            {
                if (!_config.ShowOverflowIndicators || overflowCount <= 0)
                {
                    OverflowIndicators.Visibility = Visibility.Collapsed;
                    return;
                }

                OverflowIndicators.Visibility = Visibility.Visible;

                // Show overflow count badge
                OverflowBadge.Visibility = Visibility.Visible;
                OverflowCountText.Text = $"+{overflowCount}";

                // Show appropriate scroll indicators
                HideAllScrollIndicators();

                switch (scrollPosition)
                {
                    case ScrollIndicatorPosition.Top:
                        TopScrollIndicator.Visibility = Visibility.Visible;
                        break;
                    case ScrollIndicatorPosition.Bottom:
                        BottomScrollIndicator.Visibility = Visibility.Visible;
                        break;
                    case ScrollIndicatorPosition.Left:
                        LeftScrollIndicator.Visibility = Visibility.Visible;
                        break;
                    case ScrollIndicatorPosition.Right:
                        RightScrollIndicator.Visibility = Visibility.Visible;
                        break;
                    case ScrollIndicatorPosition.None:
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing overflow indicators: {ex.Message}");
            }
        }

        /// <summary>
        /// Hides all overflow indicators
        /// </summary>
        public void HideOverflowIndicators()
        {
            try
            {
                OverflowIndicators.Visibility = Visibility.Collapsed;
                HideAllScrollIndicators();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error hiding overflow indicators: {ex.Message}");
            }
        }

        /// <summary>
        /// Hides all scroll indicators
        /// </summary>
        private void HideAllScrollIndicators()
        {
            TopScrollIndicator.Visibility = Visibility.Collapsed;
            BottomScrollIndicator.Visibility = Visibility.Collapsed;
            LeftScrollIndicator.Visibility = Visibility.Collapsed;
            RightScrollIndicator.Visibility = Visibility.Collapsed;
            OverflowBadge.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Enhanced show notification method with category and priority support
        /// </summary>
        /// <param name="notificationData">The complete notification data</param>
        public void ShowNotification(NotificationData notificationData)
        {
            try
            {
                NotificationData = notificationData;
                NotificationTitle = notificationData.Title;
                NotificationMessage = notificationData.Message;
                Timestamp = notificationData.CreatedAt.ToString("HH:mm");

                Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing notification with data: {ex.Message}");
                // Fallback to basic display
                ShowNotification(notificationData.Title, notificationData.Message);
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Handles window closing to ensure proper cleanup
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            StopAutoCloseTimer();
            StopProgressAnimation();
            base.OnClosed(e);
        }

        #endregion
    }
}