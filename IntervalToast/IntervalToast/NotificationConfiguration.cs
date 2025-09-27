using System;
using System.Windows.Media;

namespace IntervalToast
{
    /// <summary>
    /// Configuration class for notification window appearance and behavior settings.
    /// Provides customizable options for size, colors, fonts, positioning, and timing.
    /// </summary>
    public class NotificationConfiguration
    {
        #region Size and Layout

        /// <summary>
        /// Gets or sets the width of the notification window
        /// </summary>
        public double Width { get; set; } = 350;

        /// <summary>
        /// Gets or sets the height of the notification window
        /// </summary>
        public double Height { get; set; } = 120;

        /// <summary>
        /// Gets or sets the corner radius for rounded corners
        /// </summary>
        public double CornerRadius { get; set; } = 8;

        /// <summary>
        /// Gets or sets the overall opacity of the notification
        /// </summary>
        public double Opacity { get; set; } = 0.95;

        #endregion

        #region Colors

        /// <summary>
        /// Gets or sets the background color of the notification
        /// </summary>
        public System.Windows.Media.Color BackgroundColor { get; set; } = System.Windows.Media.Color.FromRgb(240, 240, 240);

        /// <summary>
        /// Gets or sets the title text color
        /// </summary>
        public System.Windows.Media.Color TitleColor { get; set; } = System.Windows.Media.Color.FromRgb(51, 51, 51);

        /// <summary>
        /// Gets or sets the message text color
        /// </summary>
        public System.Windows.Media.Color MessageColor { get; set; } = System.Windows.Media.Color.FromRgb(85, 85, 85);

        /// <summary>
        /// Gets or sets the accent color for icons and highlights
        /// </summary>
        public System.Windows.Media.Color AccentColor { get; set; } = System.Windows.Media.Color.FromRgb(74, 144, 226);

        #endregion

        #region Typography

        /// <summary>
        /// Gets or sets the font family for the title text
        /// </summary>
        public string TitleFontFamily { get; set; } = "Segoe UI";

        /// <summary>
        /// Gets or sets the font size for the title text
        /// </summary>
        public double TitleFontSize { get; set; } = 14;

        /// <summary>
        /// Gets or sets the font family for the message text
        /// </summary>
        public string MessageFontFamily { get; set; } = "Segoe UI";

        /// <summary>
        /// Gets or sets the font size for the message text
        /// </summary>
        public double MessageFontSize { get; set; } = 12;

        #endregion

        #region Positioning and Behavior

        /// <summary>
        /// Gets or sets the position where notifications should appear
        /// </summary>
        public NotificationPosition Position { get; set; } = NotificationPosition.BottomRight;

        /// <summary>
        /// Gets or sets the auto-close delay. Set to TimeSpan.Zero to disable auto-close.
        /// </summary>
        public TimeSpan AutoCloseDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets whether the notification should pause auto-close on mouse hover
        /// </summary>
        public bool PauseOnHover { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show the close button
        /// </summary>
        public bool ShowCloseButton { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show timestamps
        /// </summary>
        public bool ShowTimestamp { get; set; } = true;

        #endregion

        #region Animation Settings

        /// <summary>
        /// Gets or sets the animation duration for show/hide effects
        /// </summary>
        public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(300);

        /// <summary>
        /// Gets or sets whether to enable slide-in animation
        /// </summary>
        public bool EnableSlideAnimation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable fade-in animation
        /// </summary>
        public bool EnableFadeAnimation { get; set; } = true;

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a default configuration with standard settings
        /// </summary>
        /// <returns>A new NotificationConfiguration with default values</returns>
        public static NotificationConfiguration CreateDefault()
        {
            return new NotificationConfiguration();
        }

        /// <summary>
        /// Creates a dark theme configuration
        /// </summary>
        /// <returns>A new NotificationConfiguration with dark theme colors</returns>
        public static NotificationConfiguration CreateDarkTheme()
        {
            return new NotificationConfiguration
            {
                BackgroundColor = System.Windows.Media.Color.FromRgb(45, 45, 45),
                TitleColor = System.Windows.Media.Color.FromRgb(220, 220, 220),
                MessageColor = System.Windows.Media.Color.FromRgb(180, 180, 180),
                AccentColor = System.Windows.Media.Color.FromRgb(100, 160, 255)
            };
        }

        /// <summary>
        /// Creates a light theme configuration
        /// </summary>
        /// <returns>A new NotificationConfiguration with light theme colors</returns>
        public static NotificationConfiguration CreateLightTheme()
        {
            return new NotificationConfiguration
            {
                BackgroundColor = System.Windows.Media.Color.FromRgb(255, 255, 255),
                TitleColor = System.Windows.Media.Color.FromRgb(33, 33, 33),
                MessageColor = System.Windows.Media.Color.FromRgb(66, 66, 66),
                AccentColor = System.Windows.Media.Color.FromRgb(0, 122, 255)
            };
        }

        /// <summary>
        /// Creates a minimal configuration with reduced visual elements
        /// </summary>
        /// <returns>A new NotificationConfiguration with minimal styling</returns>
        public static NotificationConfiguration CreateMinimal()
        {
            return new NotificationConfiguration
            {
                Width = 300,
                Height = 80,
                CornerRadius = 4,
                ShowCloseButton = false,
                ShowTimestamp = false,
                AutoCloseDelay = TimeSpan.FromSeconds(3),
                BackgroundColor = System.Windows.Media.Color.FromRgb(248, 248, 248),
                TitleColor = System.Windows.Media.Color.FromRgb(64, 64, 64),
                MessageColor = System.Windows.Media.Color.FromRgb(96, 96, 96)
            };
        }

        /// <summary>
        /// Creates a large configuration for important notifications
        /// </summary>
        /// <returns>A new NotificationConfiguration with larger dimensions</returns>
        public static NotificationConfiguration CreateLarge()
        {
            return new NotificationConfiguration
            {
                Width = 400,
                Height = 150,
                TitleFontSize = 16,
                MessageFontSize = 14,
                AutoCloseDelay = TimeSpan.FromSeconds(8),
                CornerRadius = 12
            };
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates the configuration settings and ensures they are within acceptable ranges
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise</returns>
        public bool IsValid()
        {
            return Width > 100 && Width < 1000 &&
                   Height > 50 && Height < 500 &&
                   CornerRadius >= 0 && CornerRadius <= 50 &&
                   Opacity >= 0.1 && Opacity <= 1.0 &&
                   TitleFontSize > 0 && TitleFontSize < 72 &&
                   MessageFontSize > 0 && MessageFontSize < 72 &&
                   !string.IsNullOrWhiteSpace(TitleFontFamily) &&
                   !string.IsNullOrWhiteSpace(MessageFontFamily);
        }

        /// <summary>
        /// Clamps the configuration values to acceptable ranges
        /// </summary>
        public void ClampValues()
        {
            Width = Math.Max(100, Math.Min(1000, Width));
            Height = Math.Max(50, Math.Min(500, Height));
            CornerRadius = Math.Max(0, Math.Min(50, CornerRadius));
            Opacity = Math.Max(0.1, Math.Min(1.0, Opacity));
            TitleFontSize = Math.Max(8, Math.Min(72, TitleFontSize));
            MessageFontSize = Math.Max(8, Math.Min(72, MessageFontSize));

            if (string.IsNullOrWhiteSpace(TitleFontFamily))
                TitleFontFamily = "Segoe UI";

            if (string.IsNullOrWhiteSpace(MessageFontFamily))
                MessageFontFamily = "Segoe UI";
        }

        #endregion

        #region Copy and Clone

        /// <summary>
        /// Creates a deep copy of the current configuration
        /// </summary>
        /// <returns>A new NotificationConfiguration with the same settings</returns>
        public NotificationConfiguration Clone()
        {
            return new NotificationConfiguration
            {
                Width = Width,
                Height = Height,
                CornerRadius = CornerRadius,
                Opacity = Opacity,
                BackgroundColor = BackgroundColor,
                TitleColor = TitleColor,
                MessageColor = MessageColor,
                AccentColor = AccentColor,
                TitleFontFamily = TitleFontFamily,
                TitleFontSize = TitleFontSize,
                MessageFontFamily = MessageFontFamily,
                MessageFontSize = MessageFontSize,
                Position = Position,
                AutoCloseDelay = AutoCloseDelay,
                PauseOnHover = PauseOnHover,
                ShowCloseButton = ShowCloseButton,
                ShowTimestamp = ShowTimestamp,
                AnimationDuration = AnimationDuration,
                EnableSlideAnimation = EnableSlideAnimation,
                EnableFadeAnimation = EnableFadeAnimation
            };
        }

        #endregion
    }
}