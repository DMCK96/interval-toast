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
        /// Gets or sets the animation preset for quick configuration
        /// </summary>
        public AnimationPreset AnimationPreset { get; set; } = AnimationPreset.Normal;

        /// <summary>
        /// Gets or sets the animation duration for show/hide effects
        /// </summary>
        public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(400);

        /// <summary>
        /// Gets or sets the entry animation type
        /// </summary>
        public EntryAnimationType EntryAnimation { get; set; } = EntryAnimationType.SlideFromRight;

        /// <summary>
        /// Gets or sets the exit animation type
        /// </summary>
        public ExitAnimationType ExitAnimation { get; set; } = ExitAnimationType.SlideToRight;

        /// <summary>
        /// Gets or sets the easing function for animations
        /// </summary>
        public AnimationEasing EasingFunction { get; set; } = AnimationEasing.EaseOut;

        /// <summary>
        /// Gets or sets whether to enable fade-in animation
        /// </summary>
        public bool EnableFadeAnimation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show progress indicator for auto-dismiss
        /// </summary>
        public bool ShowProgressIndicator { get; set; } = true;

        /// <summary>
        /// Gets or sets the progress indicator style
        /// </summary>
        public ProgressIndicatorStyle ProgressStyle { get; set; } = ProgressIndicatorStyle.BottomBar;

        /// <summary>
        /// Gets or sets whether to enable visual effects (blur, glow)
        /// </summary>
        public bool EnableVisualEffects { get; set; } = true;

        /// <summary>
        /// Gets or sets the blur radius for background effects
        /// </summary>
        public double BlurRadius { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets the glow intensity for visual effects
        /// </summary>
        public double GlowIntensity { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets whether to enable hover effects
        /// </summary>
        public bool EnableHoverEffects { get; set; } = true;

        /// <summary>
        /// Gets or sets the scale factor for hover effects
        /// </summary>
        public double HoverScaleFactor { get; set; } = 1.02;

        /// <summary>
        /// Gets or sets whether to enable repositioning animations when notifications are dismissed
        /// </summary>
        public bool EnableRepositionAnimations { get; set; } = true;

        /// <summary>
        /// Gets or sets the duration for repositioning animations
        /// </summary>
        public TimeSpan RepositionAnimationDuration { get; set; } = TimeSpan.FromMilliseconds(250);

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
                AnimationPreset = AnimationPreset,
                AnimationDuration = AnimationDuration,
                EntryAnimation = EntryAnimation,
                ExitAnimation = ExitAnimation,
                EasingFunction = EasingFunction,
                EnableFadeAnimation = EnableFadeAnimation,
                ShowProgressIndicator = ShowProgressIndicator,
                ProgressStyle = ProgressStyle,
                EnableVisualEffects = EnableVisualEffects,
                BlurRadius = BlurRadius,
                GlowIntensity = GlowIntensity,
                EnableHoverEffects = EnableHoverEffects,
                HoverScaleFactor = HoverScaleFactor,
                EnableRepositionAnimations = EnableRepositionAnimations,
                RepositionAnimationDuration = RepositionAnimationDuration
            };
        }

        #endregion

        #region Animation Presets

        /// <summary>
        /// Applies an animation preset to this configuration
        /// </summary>
        /// <param name="preset">The preset to apply</param>
        public void ApplyAnimationPreset(AnimationPreset preset)
        {
            switch (preset)
            {
                case AnimationPreset.Fast:
                    AnimationDuration = TimeSpan.FromMilliseconds(200);
                    RepositionAnimationDuration = TimeSpan.FromMilliseconds(150);
                    EasingFunction = AnimationEasing.EaseOut;
                    break;

                case AnimationPreset.Normal:
                    AnimationDuration = TimeSpan.FromMilliseconds(400);
                    RepositionAnimationDuration = TimeSpan.FromMilliseconds(250);
                    EasingFunction = AnimationEasing.EaseOut;
                    break;

                case AnimationPreset.Slow:
                    AnimationDuration = TimeSpan.FromMilliseconds(700);
                    RepositionAnimationDuration = TimeSpan.FromMilliseconds(500);
                    EasingFunction = AnimationEasing.EaseInOut;
                    break;

                case AnimationPreset.Smooth:
                    AnimationDuration = TimeSpan.FromMilliseconds(500);
                    RepositionAnimationDuration = TimeSpan.FromMilliseconds(350);
                    EasingFunction = AnimationEasing.EaseInOut;
                    EnableVisualEffects = true;
                    HoverScaleFactor = 1.03;
                    break;

                case AnimationPreset.Minimal:
                    AnimationDuration = TimeSpan.FromMilliseconds(150);
                    RepositionAnimationDuration = TimeSpan.FromMilliseconds(100);
                    EasingFunction = AnimationEasing.Linear;
                    EnableVisualEffects = false;
                    EnableHoverEffects = false;
                    ShowProgressIndicator = false;
                    break;

                case AnimationPreset.None:
                    AnimationDuration = TimeSpan.Zero;
                    RepositionAnimationDuration = TimeSpan.Zero;
                    EnableFadeAnimation = false;
                    EnableRepositionAnimations = false;
                    EnableVisualEffects = false;
                    EnableHoverEffects = false;
                    ShowProgressIndicator = false;
                    break;
            }

            AnimationPreset = preset;
        }

        #endregion
    }

    #region Animation Enums

    /// <summary>
    /// Animation preset configurations for quick setup
    /// </summary>
    public enum AnimationPreset
    {
        /// <summary>No animations</summary>
        None,
        /// <summary>Minimal, fast animations</summary>
        Minimal,
        /// <summary>Fast animations with short duration</summary>
        Fast,
        /// <summary>Normal speed animations (default)</summary>
        Normal,
        /// <summary>Slow, deliberate animations</summary>
        Slow,
        /// <summary>Smooth animations with enhanced visual effects</summary>
        Smooth,
        /// <summary>Custom animation settings</summary>
        Custom
    }

    /// <summary>
    /// Types of entry animations for notifications
    /// </summary>
    public enum EntryAnimationType
    {
        /// <summary>No entry animation</summary>
        None,
        /// <summary>Fade in from transparent</summary>
        FadeIn,
        /// <summary>Slide in from the right edge</summary>
        SlideFromRight,
        /// <summary>Slide in from the left edge</summary>
        SlideFromLeft,
        /// <summary>Slide in from the top edge</summary>
        SlideFromTop,
        /// <summary>Slide in from the bottom edge</summary>
        SlideFromBottom,
        /// <summary>Scale up from center</summary>
        ScaleUp,
        /// <summary>Bounce in with elastic effect</summary>
        BounceIn,
        /// <summary>Fly in with rotation</summary>
        FlyIn
    }

    /// <summary>
    /// Types of exit animations for notifications
    /// </summary>
    public enum ExitAnimationType
    {
        /// <summary>No exit animation</summary>
        None,
        /// <summary>Fade out to transparent</summary>
        FadeOut,
        /// <summary>Slide out to the right edge</summary>
        SlideToRight,
        /// <summary>Slide out to the left edge</summary>
        SlideToLeft,
        /// <summary>Slide out to the top edge</summary>
        SlideToTop,
        /// <summary>Slide out to the bottom edge</summary>
        SlideToBottom,
        /// <summary>Scale down to center</summary>
        ScaleDown,
        /// <summary>Bounce out with elastic effect</summary>
        BounceOut,
        /// <summary>Fly out with rotation</summary>
        FlyOut
    }

    /// <summary>
    /// Animation easing functions for smooth transitions
    /// </summary>
    public enum AnimationEasing
    {
        /// <summary>Linear transition</summary>
        Linear,
        /// <summary>Ease in (slow start)</summary>
        EaseIn,
        /// <summary>Ease out (slow end)</summary>
        EaseOut,
        /// <summary>Ease in and out (slow start and end)</summary>
        EaseInOut,
        /// <summary>Bounce effect</summary>
        Bounce,
        /// <summary>Elastic effect</summary>
        Elastic,
        /// <summary>Back effect (slight overshoot)</summary>
        Back,
        /// <summary>Circular motion</summary>
        Circular,
        /// <summary>Exponential curve</summary>
        Exponential
    }

    /// <summary>
    /// Progress indicator styles for auto-dismiss countdown
    /// </summary>
    public enum ProgressIndicatorStyle
    {
        /// <summary>No progress indicator</summary>
        None,
        /// <summary>Thin bar at the bottom</summary>
        BottomBar,
        /// <summary>Thin bar at the top</summary>
        TopBar,
        /// <summary>Circular progress indicator</summary>
        CircularCorner,
        /// <summary>Circular progress overlay</summary>
        CircularCenter,
        /// <summary>Left border fill</summary>
        LeftBorder,
        /// <summary>Right border fill</summary>
        RightBorder
    }

    #endregion
}