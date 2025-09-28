using IntervalToast.Domain.Enums;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace IntervalToast.Domain.ValueObjects;

/// <summary>
/// Value object representing notification visual styling
/// </summary>
public sealed record NotificationStyle
{
    /// <summary>
    /// Gets the background color
    /// </summary>
    public Color BackgroundColor { get; init; } = Colors.White;

    /// <summary>
    /// Gets the text color
    /// </summary>
    public Color TextColor { get; init; } = Colors.Black;

    /// <summary>
    /// Gets the border color
    /// </summary>
    public Color BorderColor { get; init; } = Colors.Gray;

    /// <summary>
    /// Gets the font family name
    /// </summary>
    public string FontFamily { get; init; } = "Segoe UI";

    /// <summary>
    /// Gets the font size
    /// </summary>
    public double FontSize { get; init; } = 12.0;

    /// <summary>
    /// Gets the opacity value (0.0 to 1.0)
    /// </summary>
    public double Opacity { get; init; } = 0.95;

    /// <summary>
    /// Gets the corner radius
    /// </summary>
    public double CornerRadius { get; init; } = 8.0;

    /// <summary>
    /// Gets the border thickness
    /// </summary>
    public double BorderThickness { get; init; } = 1.0;

    /// <summary>
    /// Gets the animation type
    /// </summary>
    public AnimationType AnimationType { get; init; } = AnimationType.SlideRight;

    /// <summary>
    /// Gets the animation duration in milliseconds
    /// </summary>
    public int AnimationDurationMs { get; init; } = 300;

    /// <summary>
    /// Creates a default notification style for a given category
    /// </summary>
    public static NotificationStyle ForCategory(NotificationCategory category) => category switch
    {
        NotificationCategory.Success => new NotificationStyle
        {
            BackgroundColor = Color.FromRgb(212, 237, 218),
            TextColor = Color.FromRgb(21, 87, 36),
            BorderColor = Color.FromRgb(177, 196, 168)
        },
        NotificationCategory.Warning => new NotificationStyle
        {
            BackgroundColor = Color.FromRgb(255, 243, 205),
            TextColor = Color.FromRgb(102, 77, 3),
            BorderColor = Color.FromRgb(255, 193, 7)
        },
        NotificationCategory.Error => new NotificationStyle
        {
            BackgroundColor = Color.FromRgb(248, 215, 218),
            TextColor = Color.FromRgb(114, 28, 36),
            BorderColor = Color.FromRgb(220, 53, 69)
        },
        NotificationCategory.System => new NotificationStyle
        {
            BackgroundColor = Color.FromRgb(209, 236, 241),
            TextColor = Color.FromRgb(12, 84, 96),
            BorderColor = Color.FromRgb(91, 192, 222)
        },
        _ => new NotificationStyle() // Default Info style
    };
}