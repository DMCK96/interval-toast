using IntervalToast.Domain.Entities;
using IntervalToast.Domain.ValueObjects;

namespace IntervalToast.Application.Interfaces;

/// <summary>
/// Service interface for hotkey management
/// </summary>
public interface IHotkeyService
{
    /// <summary>
    /// Registers a hotkey binding with the system
    /// </summary>
    Task<bool> RegisterHotkeyAsync(HotkeyBinding binding, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a hotkey binding from the system
    /// </summary>
    Task<bool> UnregisterHotkeyAsync(Guid bindingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters all hotkey bindings
    /// </summary>
    Task UnregisterAllHotkeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered hotkey bindings
    /// </summary>
    Task<IEnumerable<HotkeyBinding>> GetRegisteredHotkeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a hotkey definition is available for registration
    /// </summary>
    Task<bool> IsHotkeyAvailableAsync(HotkeyDefinition hotkeyDefinition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets usage statistics for hotkey bindings
    /// </summary>
    Task<IEnumerable<HotkeyUsageStatistics>> GetUsageStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests if a hotkey can be registered without actually registering it
    /// </summary>
    Task<HotkeyValidationResult> ValidateHotkeyAsync(HotkeyDefinition hotkeyDefinition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a hotkey is pressed
    /// </summary>
    event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    /// <summary>
    /// Event raised when a hotkey registration fails
    /// </summary>
    event EventHandler<HotkeyRegistrationFailedEventArgs>? HotkeyRegistrationFailed;
}

/// <summary>
/// Event arguments for hotkey pressed event
/// </summary>
public sealed class HotkeyPressedEventArgs : EventArgs
{
    public HotkeyBinding Binding { get; }
    public DateTimeOffset PressedAt { get; }

    public HotkeyPressedEventArgs(HotkeyBinding binding, DateTimeOffset pressedAt)
    {
        Binding = binding;
        PressedAt = pressedAt;
    }
}

/// <summary>
/// Event arguments for hotkey registration failed event
/// </summary>
public sealed class HotkeyRegistrationFailedEventArgs : EventArgs
{
    public HotkeyBinding Binding { get; }
    public string ErrorMessage { get; }
    public Exception? Exception { get; }

    public HotkeyRegistrationFailedEventArgs(HotkeyBinding binding, string errorMessage, Exception? exception = null)
    {
        Binding = binding;
        ErrorMessage = errorMessage;
        Exception = exception;
    }
}

/// <summary>
/// Hotkey usage statistics
/// </summary>
public sealed record HotkeyUsageStatistics
{
    public Guid BindingId { get; init; }
    public string Name { get; init; } = string.Empty;
    public HotkeyDefinition HotkeyDefinition { get; init; } = null!;
    public int UsageCount { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Hotkey validation result
/// </summary>
public sealed record HotkeyValidationResult
{
    public bool IsValid { get; init; }
    public bool IsAvailable { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> Warnings { get; init; } = new();
}