using IntervalToast.Domain.ValueObjects;
using System.Windows;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using Rect = System.Windows.Rect;

namespace IntervalToast.Application.Interfaces;

/// <summary>
/// Service interface for window positioning and management
/// </summary>
public interface IWindowPositionService
{
    /// <summary>
    /// Calculates the position for a new notification window
    /// </summary>
    Point CalculateNotificationPosition(Size windowSize, int existingNotificationCount = 0);

    /// <summary>
    /// Repositions existing notifications when one is dismissed
    /// </summary>
    Task RepositionNotificationsAsync(IEnumerable<Window> activeWindows, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the working area for a specific screen
    /// </summary>
    Rect GetWorkingArea(int screenIndex = 0);

    /// <summary>
    /// Gets all available screens
    /// </summary>
    IEnumerable<ScreenInfo> GetAvailableScreens();

    /// <summary>
    /// Sets the window to be topmost without stealing focus
    /// </summary>
    void SetTopMostWithoutFocus(Window window);

    /// <summary>
    /// Calculates optimal window size based on content
    /// </summary>
    Size CalculateOptimalSize(string title, string message, NotificationStyle style);

    /// <summary>
    /// Ensures a window is visible on screen
    /// </summary>
    Point EnsureOnScreen(Point position, Size windowSize);

    /// <summary>
    /// Gets the preferred screen for notifications
    /// </summary>
    int GetPreferredScreen();
}

/// <summary>
/// Information about a screen/monitor
/// </summary>
public sealed record ScreenInfo
{
    public int Index { get; init; }
    public string Name { get; init; } = string.Empty;
    public Rect Bounds { get; init; }
    public Rect WorkingArea { get; init; }
    public bool IsPrimary { get; init; }
    public double DpiX { get; init; }
    public double DpiY { get; init; }
}