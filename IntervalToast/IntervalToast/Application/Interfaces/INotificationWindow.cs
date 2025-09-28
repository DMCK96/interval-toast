using System.Windows;

namespace IntervalToast.Application.Interfaces;

/// <summary>
/// Interface for notification window abstraction
/// </summary>
public interface INotificationWindow
{
    /// <summary>
    /// Gets or sets the width of the window
    /// </summary>
    double Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the window
    /// </summary>
    double Height { get; set; }

    /// <summary>
    /// Gets or sets the left position of the window
    /// </summary>
    double Left { get; set; }

    /// <summary>
    /// Gets or sets the top position of the window
    /// </summary>
    double Top { get; set; }

    /// <summary>
    /// Gets a value indicating whether the window is visible
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Shows the notification
    /// </summary>
    void ShowNotification();

    /// <summary>
    /// Closes the notification
    /// </summary>
    void CloseNotification();
}