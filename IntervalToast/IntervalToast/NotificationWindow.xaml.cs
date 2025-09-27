using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
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
        /// Handles mouse enter event to pause auto-close
        /// </summary>
        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            // Pause auto-close timer when mouse is over the notification
            _autoCloseTimer?.Stop();
        }

        /// <summary>
        /// Handles mouse leave event to resume auto-close
        /// </summary>
        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            // Resume auto-close timer when mouse leaves the notification
            if (_config.AutoCloseDelay > TimeSpan.Zero && !_isClosing)
            {
                _autoCloseTimer?.Start();
            }
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
        /// Closes the notification window properly
        /// </summary>
        public void CloseNotification()
        {
            if (_isClosing) return;

            _isClosing = true;
            StopAutoCloseTimer();

            // Raise the closed event
            NotificationClosed?.Invoke(this, EventArgs.Empty);

            // Close the window
            Close();
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Handles window closing to ensure proper cleanup
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            StopAutoCloseTimer();
            base.OnClosed(e);
        }

        #endregion
    }
}