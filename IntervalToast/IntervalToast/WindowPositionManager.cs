using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;

namespace IntervalToast
{
    /// <summary>
    /// Manages window positioning for notification windows, handling multi-monitor scenarios
    /// and calculating proper screen working areas accounting for taskbars and other UI elements.
    /// </summary>
    public class WindowPositionManager
    {
        #region Win32 API Declarations

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        #endregion

        #region Fields

        private static readonly List<NotificationWindow> _activeNotifications = new();
        private const double NotificationSpacing = 10.0;

        #endregion

        #region Public Methods

        /// <summary>
        /// Calculates the position for a notification window based on the specified position preference
        /// </summary>
        /// <param name="notificationWindow">The notification window to position</param>
        /// <param name="position">The desired position</param>
        /// <returns>The calculated point for the window</returns>
        public System.Windows.Point CalculatePosition(NotificationWindow notificationWindow, NotificationPosition position)
        {
            var screen = GetTargetScreen();
            var workingArea = screen.WorkingArea;

            // Convert to WPF coordinates
            var workingAreaRect = new Rect(
                workingArea.X,
                workingArea.Y,
                workingArea.Width,
                workingArea.Height);

            // Get window dimensions
            var windowWidth = notificationWindow.Width;
            var windowHeight = notificationWindow.Height;

            // Calculate base position
            var basePosition = CalculateBasePosition(workingAreaRect, windowWidth, windowHeight, position);

            // Adjust for existing notifications
            var adjustedPosition = AdjustForExistingNotifications(basePosition, windowHeight, position, workingAreaRect);

            // Register this notification
            RegisterNotification(notificationWindow);

            return adjustedPosition;
        }

        /// <summary>
        /// Removes a notification from the active list and repositions remaining notifications
        /// </summary>
        /// <param name="notificationWindow">The notification window being closed</param>
        public void UnregisterNotification(NotificationWindow notificationWindow)
        {
            _activeNotifications.Remove(notificationWindow);
            RepositionExistingNotifications();
        }

        /// <summary>
        /// Gets the current screen working area information
        /// </summary>
        /// <returns>Screen information including working area</returns>
        public Screen GetTargetScreen()
        {
            // Get cursor position to determine which screen to use
            if (GetCursorPos(out POINT cursorPos))
            {
                var point = new System.Drawing.Point(cursorPos.X, cursorPos.Y);
                var screen = Screen.FromPoint(point);
                return screen;
            }

            // Fallback to primary screen
            return Screen.PrimaryScreen ?? Screen.AllScreens.First();
        }

        /// <summary>
        /// Gets all available screens with their working areas
        /// </summary>
        /// <returns>Array of screen information</returns>
        public Screen[] GetAllScreens()
        {
            return Screen.AllScreens;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculates the base position without considering existing notifications
        /// </summary>
        private System.Windows.Point CalculateBasePosition(Rect workingArea, double windowWidth, double windowHeight, NotificationPosition position)
        {
            return position switch
            {
                NotificationPosition.TopRight => new System.Windows.Point(
                    workingArea.Right - windowWidth - NotificationSpacing,
                    workingArea.Top + NotificationSpacing),

                NotificationPosition.TopLeft => new System.Windows.Point(
                    workingArea.Left + NotificationSpacing,
                    workingArea.Top + NotificationSpacing),

                NotificationPosition.BottomRight => new System.Windows.Point(
                    workingArea.Right - windowWidth - NotificationSpacing,
                    workingArea.Bottom - windowHeight - NotificationSpacing),

                NotificationPosition.BottomLeft => new System.Windows.Point(
                    workingArea.Left + NotificationSpacing,
                    workingArea.Bottom - windowHeight - NotificationSpacing),

                NotificationPosition.TopCenter => new System.Windows.Point(
                    workingArea.Left + (workingArea.Width - windowWidth) / 2,
                    workingArea.Top + NotificationSpacing),

                NotificationPosition.BottomCenter => new System.Windows.Point(
                    workingArea.Left + (workingArea.Width - windowWidth) / 2,
                    workingArea.Bottom - windowHeight - NotificationSpacing),

                _ => new System.Windows.Point(
                    workingArea.Right - windowWidth - NotificationSpacing,
                    workingArea.Bottom - windowHeight - NotificationSpacing)
            };
        }

        /// <summary>
        /// Adjusts the position to account for existing notifications
        /// </summary>
        private System.Windows.Point AdjustForExistingNotifications(System.Windows.Point basePosition, double windowHeight,
            NotificationPosition position, Rect workingArea)
        {
            if (_activeNotifications.Count == 0)
                return basePosition;

            var adjustedPosition = basePosition;
            var stackOffset = (_activeNotifications.Count * (windowHeight + NotificationSpacing));

            switch (position)
            {
                case NotificationPosition.TopRight:
                case NotificationPosition.TopLeft:
                case NotificationPosition.TopCenter:
                    // Stack downward from top positions
                    adjustedPosition.Y += stackOffset;

                    // Ensure we don't go below the working area
                    if (adjustedPosition.Y + windowHeight > workingArea.Bottom)
                    {
                        adjustedPosition.Y = workingArea.Bottom - windowHeight - NotificationSpacing;
                    }
                    break;

                case NotificationPosition.BottomRight:
                case NotificationPosition.BottomLeft:
                case NotificationPosition.BottomCenter:
                    // Stack upward from bottom positions
                    adjustedPosition.Y -= stackOffset;

                    // Ensure we don't go above the working area
                    if (adjustedPosition.Y < workingArea.Top)
                    {
                        adjustedPosition.Y = workingArea.Top + NotificationSpacing;
                    }
                    break;
            }

            return adjustedPosition;
        }

        /// <summary>
        /// Registers a notification window
        /// </summary>
        private void RegisterNotification(NotificationWindow notificationWindow)
        {
            _activeNotifications.Add(notificationWindow);

            // Subscribe to the notification closed event
            notificationWindow.NotificationClosed += (sender, e) =>
            {
                if (sender is NotificationWindow window)
                {
                    UnregisterNotification(window);
                }
            };
        }

        /// <summary>
        /// Repositions existing notifications when one is closed
        /// </summary>
        private void RepositionExistingNotifications()
        {
            if (_activeNotifications.Count == 0)
                return;

            // Get the screen and working area
            var screen = GetTargetScreen();
            var workingArea = screen.WorkingArea;
            var workingAreaRect = new Rect(
                workingArea.X,
                workingArea.Y,
                workingArea.Width,
                workingArea.Height);

            // Reposition each notification
            for (int i = 0; i < _activeNotifications.Count; i++)
            {
                var notification = _activeNotifications[i];

                // Calculate new position based on index
                var basePosition = CalculateBasePosition(
                    workingAreaRect,
                    notification.Width,
                    notification.Height,
                    NotificationPosition.BottomRight); // Default position

                var stackOffset = i * (notification.Height + NotificationSpacing);
                var newPosition = new System.Windows.Point(basePosition.X, basePosition.Y - stackOffset);

                // Animate to new position (for now, just set directly)
                notification.Left = newPosition.X;
                notification.Top = newPosition.Y;
            }
        }

        #endregion
    }

    /// <summary>
    /// Enumeration of notification window positions
    /// </summary>
    public enum NotificationPosition
    {
        /// <summary>Top-right corner of the screen</summary>
        TopRight,

        /// <summary>Top-left corner of the screen</summary>
        TopLeft,

        /// <summary>Bottom-right corner of the screen</summary>
        BottomRight,

        /// <summary>Bottom-left corner of the screen</summary>
        BottomLeft,

        /// <summary>Top-center of the screen</summary>
        TopCenter,

        /// <summary>Bottom-center of the screen</summary>
        BottomCenter
    }
}