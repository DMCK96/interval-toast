using IntervalToast.Domain.Enums;
using IntervalToast.Domain.ValueObjects;
using System.Windows;

namespace IntervalToast.Application.Interfaces;

/// <summary>
/// Service interface for animation management
/// </summary>
public interface IAnimationService
{
    /// <summary>
    /// Animates a window into view
    /// </summary>
    Task AnimateInAsync(Window window, NotificationStyle style, CancellationToken cancellationToken = default);

    /// <summary>
    /// Animates a window out of view
    /// </summary>
    Task AnimateOutAsync(Window window, NotificationStyle style, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a show animation for the specified type
    /// </summary>
    System.Windows.Media.Animation.Storyboard CreateShowAnimation(AnimationType animationType,
        double fromOpacity = 0, double toOpacity = 1,
        int durationMs = 300);

    /// <summary>
    /// Creates a hide animation for the specified type
    /// </summary>
    System.Windows.Media.Animation.Storyboard CreateHideAnimation(AnimationType animationType,
        double fromOpacity = 1, double toOpacity = 0,
        int durationMs = 300);

    /// <summary>
    /// Applies hover effects to a window
    /// </summary>
    void ApplyHoverEffects(Window window, NotificationStyle style);

    /// <summary>
    /// Removes hover effects from a window
    /// </summary>
    void RemoveHoverEffects(Window window);

    /// <summary>
    /// Gets the recommended animation duration for a given type
    /// </summary>
    int GetRecommendedDuration(AnimationType animationType);

    /// <summary>
    /// Tests if an animation type is supported
    /// </summary>
    bool IsAnimationTypeSupported(AnimationType animationType);
}