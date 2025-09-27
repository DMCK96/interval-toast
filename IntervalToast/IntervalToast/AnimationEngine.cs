using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace IntervalToast
{
    /// <summary>
    /// Manages all animation logic for notification windows including entry, exit, hover, and repositioning animations.
    /// Provides a centralized system for creating and managing WPF Storyboards with various easing functions.
    /// </summary>
    public static class AnimationEngine
    {
        #region Public Animation Methods

        /// <summary>
        /// Creates and starts an entry animation for a notification window
        /// </summary>
        /// <param name="window">The notification window to animate</param>
        /// <param name="config">The animation configuration</param>
        /// <param name="onCompleted">Optional callback when animation completes</param>
        public static void AnimateEntry(NotificationWindow window, NotificationConfiguration config, EventHandler? onCompleted = null)
        {
            try
            {
                if (window == null)
                    throw new ArgumentNullException(nameof(window));
                if (config == null)
                    throw new ArgumentNullException(nameof(config));

                if (config.AnimationDuration == TimeSpan.Zero || config.EntryAnimation == EntryAnimationType.None)
                {
                    onCompleted?.Invoke(window, EventArgs.Empty);
                    return;
                }

                var storyboard = CreateEntryStoryboard(window, config);

                if (onCompleted != null)
                {
                    storyboard.Completed += onCompleted;
                }

                storyboard.Begin();
            }
            catch (Exception ex)
            {
                // Log the error and execute callback to prevent hanging
                System.Diagnostics.Debug.WriteLine($"AnimateEntry error: {ex.Message}");
                onCompleted?.Invoke(window, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Creates and starts an exit animation for a notification window
        /// </summary>
        /// <param name="window">The notification window to animate</param>
        /// <param name="config">The animation configuration</param>
        /// <param name="onCompleted">Optional callback when animation completes</param>
        public static void AnimateExit(NotificationWindow window, NotificationConfiguration config, EventHandler? onCompleted = null)
        {
            try
            {
                if (window == null)
                    throw new ArgumentNullException(nameof(window));
                if (config == null)
                    throw new ArgumentNullException(nameof(config));

                if (config.AnimationDuration == TimeSpan.Zero || config.ExitAnimation == ExitAnimationType.None)
                {
                    onCompleted?.Invoke(window, EventArgs.Empty);
                    return;
                }

                var storyboard = CreateExitStoryboard(window, config);

                if (onCompleted != null)
                {
                    storyboard.Completed += onCompleted;
                }

                storyboard.Begin();
            }
            catch (Exception ex)
            {
                // Log the error and execute callback to prevent hanging
                System.Diagnostics.Debug.WriteLine($"AnimateExit error: {ex.Message}");
                onCompleted?.Invoke(window, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Creates and starts a hover animation for enhanced user interaction
        /// </summary>
        /// <param name="window">The notification window to animate</param>
        /// <param name="config">The animation configuration</param>
        /// <param name="isEntering">True for mouse enter, false for mouse leave</param>
        public static void AnimateHover(NotificationWindow window, NotificationConfiguration config, bool isEntering)
        {
            try
            {
                if (window == null || config == null || !config.EnableHoverEffects)
                    return;

                var storyboard = CreateHoverStoryboard(window, config, isEntering);
                storyboard.Begin();
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - hover effects are non-critical
                System.Diagnostics.Debug.WriteLine($"AnimateHover error: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates and starts a repositioning animation for smooth window movement
        /// </summary>
        /// <param name="window">The notification window to animate</param>
        /// <param name="targetPosition">The target position to animate to</param>
        /// <param name="config">The animation configuration</param>
        /// <param name="onCompleted">Optional callback when animation completes</param>
        public static void AnimateReposition(NotificationWindow window, System.Windows.Point targetPosition, NotificationConfiguration config, EventHandler? onCompleted = null)
        {
            try
            {
                if (window == null || config == null)
                {
                    onCompleted?.Invoke(window, EventArgs.Empty);
                    return;
                }

                if (!config.EnableRepositionAnimations || config.RepositionAnimationDuration == TimeSpan.Zero)
                {
                    window.Left = targetPosition.X;
                    window.Top = targetPosition.Y;
                    onCompleted?.Invoke(window, EventArgs.Empty);
                    return;
                }

                var storyboard = CreateRepositionStoryboard(window, targetPosition, config);

                if (onCompleted != null)
                {
                    storyboard.Completed += onCompleted;
                }

                storyboard.Begin();
            }
            catch (Exception ex)
            {
                // Log the error and fallback to direct positioning
                System.Diagnostics.Debug.WriteLine($"AnimateReposition error: {ex.Message}");
                try
                {
                    window.Left = targetPosition.X;
                    window.Top = targetPosition.Y;
                }
                catch
                {
                    // Ignore positioning errors
                }
                onCompleted?.Invoke(window, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Creates a progress animation for auto-dismiss countdown
        /// </summary>
        /// <param name="progressElement">The UI element representing progress</param>
        /// <param name="duration">The total duration of the countdown</param>
        /// <param name="style">The progress indicator style</param>
        /// <param name="onCompleted">Optional callback when countdown completes</param>
        public static Storyboard CreateProgressAnimation(FrameworkElement progressElement, TimeSpan duration,
            ProgressIndicatorStyle style, EventHandler? onCompleted = null)
        {
            try
            {
                if (progressElement == null)
                    return new Storyboard();

                var storyboard = new Storyboard();

            switch (style)
            {
                case ProgressIndicatorStyle.BottomBar:
                case ProgressIndicatorStyle.TopBar:
                case ProgressIndicatorStyle.LeftBorder:
                case ProgressIndicatorStyle.RightBorder:
                    var widthAnimation = CreateProgressBarAnimation(duration);
                    Storyboard.SetTarget(widthAnimation, progressElement);
                    Storyboard.SetTargetProperty(widthAnimation, new PropertyPath("Width"));
                    storyboard.Children.Add(widthAnimation);
                    break;

                case ProgressIndicatorStyle.CircularCorner:
                case ProgressIndicatorStyle.CircularCenter:
                    // For circular progress, we'll use a custom animation approach
                    // This creates a rotation animation instead of trying to animate stroke dash
                    var rotationAnimation = new DoubleAnimation
                    {
                        From = 0.0,
                        To = 360.0,
                        Duration = new Duration(duration),
                        EasingFunction = new PowerEase { Power = 1 }
                    };

                    // Ensure the progress element has a rotation transform
                    if (progressElement.RenderTransform is not RotateTransform)
                    {
                        progressElement.RenderTransform = new RotateTransform();
                        progressElement.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                    }

                    Storyboard.SetTarget(rotationAnimation, progressElement);
                    Storyboard.SetTargetProperty(rotationAnimation, new PropertyPath("RenderTransform.Angle"));
                    storyboard.Children.Add(rotationAnimation);
                    break;
            }

                if (onCompleted != null)
                {
                    storyboard.Completed += onCompleted;
                }

                return storyboard;
            }
            catch (Exception ex)
            {
                // Log the error and return empty storyboard
                System.Diagnostics.Debug.WriteLine($"CreateProgressAnimation error: {ex.Message}");
                var fallbackStoryboard = new Storyboard();
                if (onCompleted != null)
                {
                    fallbackStoryboard.Completed += onCompleted;
                }
                return fallbackStoryboard;
            }
        }

        #endregion

        #region Entry Animation Creation

        /// <summary>
        /// Creates a storyboard for entry animations
        /// </summary>
        private static Storyboard CreateEntryStoryboard(NotificationWindow window, NotificationConfiguration config)
        {
            try
            {
                var storyboard = new Storyboard();
                var duration = new Duration(config.AnimationDuration);
                var easing = GetEasingFunction(config.EasingFunction);

            // Fade animation (if enabled)
            if (config.EnableFadeAnimation)
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = duration,
                    EasingFunction = easing
                };

                Storyboard.SetTarget(fadeAnimation, window);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));
                storyboard.Children.Add(fadeAnimation);

                // Set initial opacity
                window.Opacity = 0.0;
            }

            // Position-based animations
            switch (config.EntryAnimation)
            {
                case EntryAnimationType.SlideFromRight:
                    CreateSlideAnimation(storyboard, window, duration, easing, window.Left + window.Width, window.Left, "Left");
                    break;

                case EntryAnimationType.SlideFromLeft:
                    CreateSlideAnimation(storyboard, window, duration, easing, window.Left - window.Width, window.Left, "Left");
                    break;

                case EntryAnimationType.SlideFromTop:
                    CreateSlideAnimation(storyboard, window, duration, easing, window.Top - window.Height, window.Top, "Top");
                    break;

                case EntryAnimationType.SlideFromBottom:
                    CreateSlideAnimation(storyboard, window, duration, easing, window.Top + window.Height, window.Top, "Top");
                    break;

                case EntryAnimationType.ScaleUp:
                    CreateScaleAnimation(storyboard, window, duration, easing, 0.0, 1.0);
                    break;

                case EntryAnimationType.BounceIn:
                    CreateBounceInAnimation(storyboard, window, duration);
                    break;

                case EntryAnimationType.FlyIn:
                    CreateFlyInAnimation(storyboard, window, duration, easing);
                    break;
            }

                return storyboard;
            }
            catch (Exception ex)
            {
                // Log the error and return a basic storyboard
                System.Diagnostics.Debug.WriteLine($"CreateEntryStoryboard error: {ex.Message}");
                return new Storyboard();
            }
        }

        #endregion

        #region Exit Animation Creation

        /// <summary>
        /// Creates a storyboard for exit animations
        /// </summary>
        private static Storyboard CreateExitStoryboard(NotificationWindow window, NotificationConfiguration config)
        {
            try
            {
                var storyboard = new Storyboard();
                var duration = new Duration(config.AnimationDuration);
                var easing = GetEasingFunction(config.EasingFunction);

            // Fade animation (if enabled)
            if (config.EnableFadeAnimation)
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = window.Opacity,
                    To = 0.0,
                    Duration = duration,
                    EasingFunction = easing
                };

                Storyboard.SetTarget(fadeAnimation, window);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));
                storyboard.Children.Add(fadeAnimation);
            }

            // Position-based animations
            switch (config.ExitAnimation)
            {
                case ExitAnimationType.SlideToRight:
                    CreateSlideAnimation(storyboard, window, duration, easing, window.Left, window.Left + window.Width, "Left");
                    break;

                case ExitAnimationType.SlideToLeft:
                    CreateSlideAnimation(storyboard, window, duration, easing, window.Left, window.Left - window.Width, "Left");
                    break;

                case ExitAnimationType.SlideToTop:
                    CreateSlideAnimation(storyboard, window, duration, easing, window.Top, window.Top - window.Height, "Top");
                    break;

                case ExitAnimationType.SlideToBottom:
                    CreateSlideAnimation(storyboard, window, duration, easing, window.Top, window.Top + window.Height, "Top");
                    break;

                case ExitAnimationType.ScaleDown:
                    CreateScaleAnimation(storyboard, window, duration, easing, 1.0, 0.0);
                    break;

                case ExitAnimationType.BounceOut:
                    CreateBounceOutAnimation(storyboard, window, duration);
                    break;

                case ExitAnimationType.FlyOut:
                    CreateFlyOutAnimation(storyboard, window, duration, easing);
                    break;
            }

                return storyboard;
            }
            catch (Exception ex)
            {
                // Log the error and return a basic storyboard
                System.Diagnostics.Debug.WriteLine($"CreateExitStoryboard error: {ex.Message}");
                return new Storyboard();
            }
        }

        #endregion

        #region Hover Animation Creation

        /// <summary>
        /// Creates a storyboard for hover effects
        /// </summary>
        private static Storyboard CreateHoverStoryboard(NotificationWindow window, NotificationConfiguration config, bool isEntering)
        {
            var storyboard = new Storyboard();
            var duration = new Duration(TimeSpan.FromMilliseconds(200));
            var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };

            var targetScale = isEntering ? config.HoverScaleFactor : 1.0;

            CreateScaleAnimation(storyboard, window, duration, easing, isEntering ? 1.0 : config.HoverScaleFactor, targetScale);

            // Optional subtle opacity change
            if (config.EnableVisualEffects)
            {
                var opacityAnimation = new DoubleAnimation
                {
                    From = isEntering ? config.Opacity : Math.Min(1.0, config.Opacity + 0.05),
                    To = isEntering ? Math.Min(1.0, config.Opacity + 0.05) : config.Opacity,
                    Duration = duration,
                    EasingFunction = easing
                };

                Storyboard.SetTarget(opacityAnimation, window);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
                storyboard.Children.Add(opacityAnimation);
            }

            return storyboard;
        }

        #endregion

        #region Repositioning Animation Creation

        /// <summary>
        /// Creates a storyboard for repositioning animations
        /// </summary>
        private static Storyboard CreateRepositionStoryboard(NotificationWindow window, System.Windows.Point targetPosition, NotificationConfiguration config)
        {
            var storyboard = new Storyboard();
            var duration = new Duration(config.RepositionAnimationDuration);
            var easing = new QuadraticEase { EasingMode = EasingMode.EaseInOut };

            // Animate Left position
            if (Math.Abs(window.Left - targetPosition.X) > 0.1)
            {
                var leftAnimation = new DoubleAnimation
                {
                    From = window.Left,
                    To = targetPosition.X,
                    Duration = duration,
                    EasingFunction = easing
                };

                Storyboard.SetTarget(leftAnimation, window);
                Storyboard.SetTargetProperty(leftAnimation, new PropertyPath("Left"));
                storyboard.Children.Add(leftAnimation);
            }

            // Animate Top position
            if (Math.Abs(window.Top - targetPosition.Y) > 0.1)
            {
                var topAnimation = new DoubleAnimation
                {
                    From = window.Top,
                    To = targetPosition.Y,
                    Duration = duration,
                    EasingFunction = easing
                };

                Storyboard.SetTarget(topAnimation, window);
                Storyboard.SetTargetProperty(topAnimation, new PropertyPath("Top"));
                storyboard.Children.Add(topAnimation);
            }

            return storyboard;
        }

        #endregion

        #region Animation Helper Methods

        /// <summary>
        /// Creates a slide animation for the specified property
        /// </summary>
        private static void CreateSlideAnimation(Storyboard storyboard, NotificationWindow window, Duration duration,
            IEasingFunction easing, double fromValue, double toValue, string propertyName)
        {
            var slideAnimation = new DoubleAnimation
            {
                From = fromValue,
                To = toValue,
                Duration = duration,
                EasingFunction = easing
            };

            Storyboard.SetTarget(slideAnimation, window);
            Storyboard.SetTargetProperty(slideAnimation, new PropertyPath(propertyName));
            storyboard.Children.Add(slideAnimation);

            // Set initial position
            if (propertyName == "Left")
                window.Left = fromValue;
            else if (propertyName == "Top")
                window.Top = fromValue;
        }

        /// <summary>
        /// Creates a scale animation using render transform
        /// </summary>
        private static void CreateScaleAnimation(Storyboard storyboard, NotificationWindow window, Duration duration,
            IEasingFunction easing, double fromScale, double toScale)
        {
            // Ensure the window has a proper transform structure
            EnsureTransformGroup(window);

            var transformGroup = (TransformGroup)window.RenderTransform;
            var scaleTransform = GetOrCreateScaleTransform(transformGroup);

            // Set initial scale
            scaleTransform.ScaleX = fromScale;
            scaleTransform.ScaleY = fromScale;

            var scaleXAnimation = new DoubleAnimation
            {
                From = fromScale,
                To = toScale,
                Duration = duration,
                EasingFunction = easing
            };

            var scaleYAnimation = new DoubleAnimation
            {
                From = fromScale,
                To = toScale,
                Duration = duration,
                EasingFunction = easing
            };

            // Get the index of the scale transform in the group
            var scaleIndex = transformGroup.Children.IndexOf(scaleTransform);

            Storyboard.SetTarget(scaleXAnimation, window);
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath($"RenderTransform.Children[{scaleIndex}].ScaleX"));
            storyboard.Children.Add(scaleXAnimation);

            Storyboard.SetTarget(scaleYAnimation, window);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath($"RenderTransform.Children[{scaleIndex}].ScaleY"));
            storyboard.Children.Add(scaleYAnimation);
        }

        /// <summary>
        /// Creates a bounce-in animation with elastic effect
        /// </summary>
        private static void CreateBounceInAnimation(Storyboard storyboard, NotificationWindow window, Duration duration)
        {
            var elasticEasing = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 2, Springiness = 0.3 };
            CreateScaleAnimation(storyboard, window, duration, elasticEasing, 0.0, 1.0);
        }

        /// <summary>
        /// Creates a bounce-out animation with elastic effect
        /// </summary>
        private static void CreateBounceOutAnimation(Storyboard storyboard, NotificationWindow window, Duration duration)
        {
            var elasticEasing = new ElasticEase { EasingMode = EasingMode.EaseIn, Oscillations = 1, Springiness = 0.5 };
            CreateScaleAnimation(storyboard, window, duration, elasticEasing, 1.0, 0.0);
        }

        /// <summary>
        /// Creates a fly-in animation with rotation
        /// </summary>
        private static void CreateFlyInAnimation(Storyboard storyboard, NotificationWindow window, Duration duration, IEasingFunction easing)
        {
            // Ensure the window has a proper transform structure
            EnsureTransformGroup(window);

            var transformGroup = (TransformGroup)window.RenderTransform;
            var scaleTransform = GetOrCreateScaleTransform(transformGroup);
            var rotateTransform = GetOrCreateRotateTransform(transformGroup);

            // Set initial values
            scaleTransform.ScaleX = 0.3;
            scaleTransform.ScaleY = 0.3;
            rotateTransform.Angle = -45;

            // Create scale animation
            CreateScaleAnimation(storyboard, window, duration, easing, 0.3, 1.0);

            // Create rotation animation
            var rotateAnimation = new DoubleAnimation
            {
                From = -45,
                To = 0,
                Duration = duration,
                EasingFunction = easing
            };

            var rotateIndex = transformGroup.Children.IndexOf(rotateTransform);
            Storyboard.SetTarget(rotateAnimation, window);
            Storyboard.SetTargetProperty(rotateAnimation, new PropertyPath($"RenderTransform.Children[{rotateIndex}].Angle"));
            storyboard.Children.Add(rotateAnimation);
        }

        /// <summary>
        /// Creates a fly-out animation with rotation
        /// </summary>
        private static void CreateFlyOutAnimation(Storyboard storyboard, NotificationWindow window, Duration duration, IEasingFunction easing)
        {
            // Ensure the window has a proper transform structure
            EnsureTransformGroup(window);

            var transformGroup = (TransformGroup)window.RenderTransform;
            var rotateTransform = GetOrCreateRotateTransform(transformGroup);

            CreateScaleAnimation(storyboard, window, duration, easing, 1.0, 0.3);

            var rotateAnimation = new DoubleAnimation
            {
                From = rotateTransform.Angle,
                To = 45,
                Duration = duration,
                EasingFunction = easing
            };

            var rotateIndex = transformGroup.Children.IndexOf(rotateTransform);
            Storyboard.SetTarget(rotateAnimation, window);
            Storyboard.SetTargetProperty(rotateAnimation, new PropertyPath($"RenderTransform.Children[{rotateIndex}].Angle"));
            storyboard.Children.Add(rotateAnimation);
        }

        /// <summary>
        /// Creates a progress bar animation
        /// </summary>
        private static DoubleAnimation CreateProgressBarAnimation(TimeSpan duration)
        {
            return new DoubleAnimation
            {
                From = 0.0,
                To = 100.0, // Will be bound to the actual width
                Duration = new Duration(duration),
                EasingFunction = new PowerEase { Power = 1 }
            };
        }

        /// <summary>
        /// Creates a circular progress animation
        /// </summary>
        private static DoubleAnimation CreateCircularProgressAnimation(TimeSpan duration)
        {
            // For circular progress, we'll animate from 0,100 to 100,0 (StrokeDashArray)
            return new DoubleAnimation
            {
                From = 0.0,
                To = 100.0,
                Duration = new Duration(duration),
                EasingFunction = new PowerEase { Power = 1 }
            };
        }

        /// <summary>
        /// Gets the appropriate WPF easing function for the specified animation easing type
        /// </summary>
        private static IEasingFunction GetEasingFunction(AnimationEasing easing)
        {
            return easing switch
            {
                AnimationEasing.Linear => new PowerEase { Power = 1 },
                AnimationEasing.EaseIn => new QuadraticEase { EasingMode = EasingMode.EaseIn },
                AnimationEasing.EaseOut => new QuadraticEase { EasingMode = EasingMode.EaseOut },
                AnimationEasing.EaseInOut => new QuadraticEase { EasingMode = EasingMode.EaseInOut },
                AnimationEasing.Bounce => new BounceEase { EasingMode = EasingMode.EaseOut },
                AnimationEasing.Elastic => new ElasticEase { EasingMode = EasingMode.EaseOut },
                AnimationEasing.Back => new BackEase { EasingMode = EasingMode.EaseOut },
                AnimationEasing.Circular => new CircleEase { EasingMode = EasingMode.EaseOut },
                AnimationEasing.Exponential => new ExponentialEase { EasingMode = EasingMode.EaseOut },
                _ => new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
        }

        /// <summary>
        /// Ensures the window has a proper TransformGroup for safe animation
        /// </summary>
        private static void EnsureTransformGroup(NotificationWindow window)
        {
            try
            {
                if (window?.RenderTransform is not TransformGroup && window != null)
                {
                    window.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

                    var transformGroup = new TransformGroup();

                    // Preserve existing transform if it's a simple one
                    if (window.RenderTransform != null && window.RenderTransform != Transform.Identity)
                    {
                        transformGroup.Children.Add(window.RenderTransform);
                    }

                    window.RenderTransform = transformGroup;
                }
            }
            catch (Exception ex)
            {
                // Log the error and set a basic transform group
                System.Diagnostics.Debug.WriteLine($"EnsureTransformGroup error: {ex.Message}");
                try
                {
                    window.RenderTransform = new TransformGroup();
                    window.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                }
                catch
                {
                    // Ignore if we can't set the transform
                }
            }
        }

        /// <summary>
        /// Gets or creates a ScaleTransform in the TransformGroup
        /// </summary>
        private static ScaleTransform GetOrCreateScaleTransform(TransformGroup transformGroup)
        {
            if (transformGroup == null)
                return new ScaleTransform(1.0, 1.0);

            var scaleTransform = transformGroup.Children.OfType<ScaleTransform>().FirstOrDefault();
            if (scaleTransform == null)
            {
                scaleTransform = new ScaleTransform(1.0, 1.0);
                transformGroup.Children.Add(scaleTransform);
            }
            return scaleTransform;
        }

        /// <summary>
        /// Gets or creates a RotateTransform in the TransformGroup
        /// </summary>
        private static RotateTransform GetOrCreateRotateTransform(TransformGroup transformGroup)
        {
            if (transformGroup == null)
                return new RotateTransform(0.0);

            var rotateTransform = transformGroup.Children.OfType<RotateTransform>().FirstOrDefault();
            if (rotateTransform == null)
            {
                rotateTransform = new RotateTransform(0.0);
                transformGroup.Children.Add(rotateTransform);
            }
            return rotateTransform;
        }

        /// <summary>
        /// Gets or creates a TranslateTransform in the TransformGroup
        /// </summary>
        private static TranslateTransform GetOrCreateTranslateTransform(TransformGroup transformGroup)
        {
            if (transformGroup == null)
                return new TranslateTransform(0.0, 0.0);

            var translateTransform = transformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();
            if (translateTransform == null)
            {
                translateTransform = new TranslateTransform(0.0, 0.0);
                transformGroup.Children.Add(translateTransform);
            }
            return translateTransform;
        }

        #endregion
    }
}