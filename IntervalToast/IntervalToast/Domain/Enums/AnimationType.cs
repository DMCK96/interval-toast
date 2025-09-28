namespace IntervalToast.Domain.Enums;

/// <summary>
/// Defines animation types for notification display
/// </summary>
public enum AnimationType
{
    /// <summary>No animation</summary>
    None,

    /// <summary>Fade in/out animation</summary>
    Fade,

    /// <summary>Slide from right</summary>
    SlideRight,

    /// <summary>Slide from left</summary>
    SlideLeft,

    /// <summary>Slide from top</summary>
    SlideTop,

    /// <summary>Slide from bottom</summary>
    SlideBottom,

    /// <summary>Scale animation</summary>
    Scale,

    /// <summary>Bounce animation</summary>
    Bounce
}