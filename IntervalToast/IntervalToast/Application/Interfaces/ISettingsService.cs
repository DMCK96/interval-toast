using IntervalToast.Domain.Entities;

namespace IntervalToast.Application.Interfaces;

/// <summary>
/// Service interface for application settings management
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current application settings
    /// </summary>
    Task<ApplicationSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the application settings
    /// </summary>
    Task UpdateSettingsAsync(ApplicationSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets settings to default values
    /// </summary>
    Task<ApplicationSettings> ResetToDefaultsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a backup of current settings
    /// </summary>
    Task<string> CreateBackupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores settings from a backup file
    /// </summary>
    Task<ApplicationSettings> RestoreFromBackupAsync(string backupFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the current settings file
    /// </summary>
    Task<SettingsValidationResult> ValidateSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports settings to a file
    /// </summary>
    Task ExportSettingsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports settings from a file
    /// </summary>
    Task<ApplicationSettings> ImportSettingsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when settings are updated
    /// </summary>
    event EventHandler<SettingsUpdatedEventArgs>? SettingsUpdated;
}

/// <summary>
/// Event arguments for settings updated event
/// </summary>
public sealed class SettingsUpdatedEventArgs : EventArgs
{
    public ApplicationSettings Settings { get; }
    public DateTimeOffset UpdatedAt { get; }

    public SettingsUpdatedEventArgs(ApplicationSettings settings, DateTimeOffset updatedAt)
    {
        Settings = settings;
        UpdatedAt = updatedAt;
    }
}

/// <summary>
/// Settings validation result
/// </summary>
public sealed record SettingsValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public bool RequiresBackup { get; init; }
}