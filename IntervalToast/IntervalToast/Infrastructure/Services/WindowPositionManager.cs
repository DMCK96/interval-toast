using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using IntervalToast.Application.Interfaces;
using IntervalToast.Domain.Entities;
using IntervalToast.Domain.Enums;

namespace IntervalToast.Infrastructure.Services
{
    /// <summary>
    /// Window positioning manager supporting vertical stacking of multiple notifications.
    /// Handles screen positioning, vertical stacking, and repositioning animations.
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

        private const double DefaultNotificationSpacing = 10.0;
        private const double UniformNotificationWidth = 350.0;
        private const double UniformNotificationHeight = 120.0;

        #endregion

        #region Public Methods

        /// <summary>
        /// Calculates the position for a notification window with vertical stacking support.
        /// Always uses uniform sizing and proper vertical positioning based on stack index.
        /// </summary>
        /// <param name="notificationWindow">The notification window to position</param>
        /// <param name="position">The desired position</param>
        /// <param name="stackIndex">The index in the vertical stack (0 = topmost/newest)</param>
        /// <param name="configuration">Optional configuration (size settings ignored for uniformity)</param>
        /// <returns>The calculated point for the window</returns>
        internal System.Windows.Point CalculatePosition(INotificationWindow notificationWindow, NotificationPosition position, int stackIndex = 0, NotificationConfiguration? configuration = null)
        {
            var screen = GetTargetScreen();
            var workingArea = screen.WorkingArea;

            // Convert to WPF coordinates
            var workingAreaRect = new Rect(
                workingArea.X,
                workingArea.Y,
                workingArea.Width,
                workingArea.Height);

            // Apply uniform sizing to ensure consistency
            notificationWindow.Width = UniformNotificationWidth;
            notificationWindow.Height = UniformNotificationHeight;

            // Calculate base position for the stack
            var basePosition = CalculateBasePosition(workingAreaRect, UniformNotificationWidth, UniformNotificationHeight, position);

            // Apply vertical stacking offset
            var finalPosition = CalculateStackedPosition(basePosition, stackIndex, position, configuration);

            return finalPosition;
        }

        /// <summary>
        /// Legacy method for backward compatibility - single notification positioning
        /// </summary>
        internal System.Windows.Point CalculatePosition(INotificationWindow notificationWindow, NotificationPosition position)
        {
            return CalculatePosition(notificationWindow, position, 0, null);
        }

        /// <summary>
        /// Calculates positions for multiple notifications in a vertical stack
        /// </summary>
        /// <param name="notificationCount">Number of notifications to position</param>
        /// <param name="position">The desired position anchor</param>
        /// <param name="configuration">Optional configuration</param>
        /// <returns>Array of calculated positions for each notification</returns>
        public System.Windows.Point[] CalculateStackPositions(int notificationCount, NotificationPosition position, NotificationConfiguration? configuration = null)
        {
            var positions = new System.Windows.Point[notificationCount];
            var screen = GetTargetScreen();
            var workingArea = screen.WorkingArea;

            var workingAreaRect = new Rect(
                workingArea.X,
                workingArea.Y,
                workingArea.Width,
                workingArea.Height);

            var basePosition = CalculateBasePosition(workingAreaRect, UniformNotificationWidth, UniformNotificationHeight, position);

            for (int i = 0; i < notificationCount; i++)
            {
                positions[i] = CalculateStackedPosition(basePosition, i, position, configuration);
            }

            return positions;
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
                    workingArea.Right - windowWidth - DefaultNotificationSpacing,
                    workingArea.Top + DefaultNotificationSpacing),

                NotificationPosition.TopLeft => new System.Windows.Point(
                    workingArea.Left + DefaultNotificationSpacing,
                    workingArea.Top + DefaultNotificationSpacing),

                NotificationPosition.BottomRight => new System.Windows.Point(
                    workingArea.Right - windowWidth - DefaultNotificationSpacing,
                    workingArea.Bottom - windowHeight - DefaultNotificationSpacing),

                NotificationPosition.BottomLeft => new System.Windows.Point(
                    workingArea.Left + DefaultNotificationSpacing,
                    workingArea.Bottom - windowHeight - DefaultNotificationSpacing),

                NotificationPosition.TopCenter => new System.Windows.Point(
                    workingArea.Left + (workingArea.Width - windowWidth) / 2,
                    workingArea.Top + DefaultNotificationSpacing),

                NotificationPosition.BottomCenter => new System.Windows.Point(
                    workingArea.Left + (workingArea.Width - windowWidth) / 2,
                    workingArea.Bottom - windowHeight - DefaultNotificationSpacing),

                _ => new System.Windows.Point(
                    workingArea.Right - windowWidth - DefaultNotificationSpacing,
                    workingArea.Bottom - windowHeight - DefaultNotificationSpacing)
            };
        }





        /// <summary>
        /// Calculates the actual position for a notification in a vertical stack
        /// </summary>
        /// <param name="basePosition">The base position for the stack</param>
        /// <param name="stackIndex">The index in the stack (0 = anchor position)</param>
        /// <param name="position">The notification position type</param>
        /// <param name="configuration">Optional configuration</param>
        /// <returns>The final calculated position</returns>
        private System.Windows.Point CalculateStackedPosition(System.Windows.Point basePosition, int stackIndex, NotificationPosition position, NotificationConfiguration? configuration)
        {
            var spacing = configuration?.NotificationSpacing ?? DefaultNotificationSpacing;
            var notificationHeight = UniformNotificationHeight;
            var totalOffset = stackIndex * (notificationHeight + spacing);

            // Determine stacking direction based on position
            return position switch
            {
                NotificationPosition.TopRight or NotificationPosition.TopLeft or NotificationPosition.TopCenter =>
                    new System.Windows.Point(basePosition.X, basePosition.Y + totalOffset),

                NotificationPosition.BottomRight or NotificationPosition.BottomLeft or NotificationPosition.BottomCenter =>
                    new System.Windows.Point(basePosition.X, basePosition.Y - totalOffset),

                _ => new System.Windows.Point(basePosition.X, basePosition.Y - totalOffset)
            };
        }

        /// <summary>
        /// Calculates the positions for repositioning existing notifications after one is removed
        /// </summary>
        /// <param name="currentPositions">Current positions of all notifications</param>
        /// <param name="removedIndex">Index of the removed notification</param>
        /// <param name="position">The notification position type</param>
        /// <param name="configuration">Optional configuration</param>
        /// <returns>New positions for remaining notifications</returns>
        public System.Windows.Point[] CalculateRepositionedPositions(System.Windows.Point[] currentPositions, int removedIndex, NotificationPosition position, NotificationConfiguration? configuration = null)
        {
            if (currentPositions == null || currentPositions.Length == 0)
                return Array.Empty<System.Windows.Point>();

            var newPositions = new List<System.Windows.Point>();
            var spacing = configuration?.NotificationSpacing ?? DefaultNotificationSpacing;
            var notificationHeight = UniformNotificationHeight;

            // Recalculate positions for remaining notifications
            for (int i = 0; i < currentPositions.Length; i++)
            {
                if (i == removedIndex) continue; // Skip the removed notification

                // Calculate new stack index (shift down notifications that were above the removed one)
                var newStackIndex = i > removedIndex ? i - 1 : i;
                var currentPos = currentPositions[i];

                // For top positions, move notifications up; for bottom positions, move them down
                var moveDistance = notificationHeight + spacing;

                System.Windows.Point newPosition = position switch
                {
                    NotificationPosition.TopRight or NotificationPosition.TopLeft or NotificationPosition.TopCenter =>
                        i > removedIndex ? new System.Windows.Point(currentPos.X, currentPos.Y - moveDistance) : currentPos,

                    NotificationPosition.BottomRight or NotificationPosition.BottomLeft or NotificationPosition.BottomCenter =>
                        i > removedIndex ? new System.Windows.Point(currentPos.X, currentPos.Y + moveDistance) : currentPos,

                    _ => i > removedIndex ? new System.Windows.Point(currentPos.X, currentPos.Y + moveDistance) : currentPos
                };

                newPositions.Add(newPosition);
            }

            return newPositions.ToArray();
        }

        /// <summary>
        /// Validates that the given stack positions fit within the screen bounds
        /// </summary>
        /// <param name="positions">The positions to validate</param>
        /// <param name="position">The notification position type</param>
        /// <returns>True if all positions fit on screen</returns>
        public bool ValidateStackPositions(System.Windows.Point[] positions, NotificationPosition position)
        {
            if (positions == null || positions.Length == 0)
                return true;

            var screen = GetTargetScreen();
            var workingArea = screen.WorkingArea;

            foreach (var pos in positions)
            {
                // Check if notification would be outside screen bounds
                if (pos.X < workingArea.Left ||
                    pos.X + UniformNotificationWidth > workingArea.Right ||
                    pos.Y < workingArea.Top ||
                    pos.Y + UniformNotificationHeight > workingArea.Bottom)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }

}