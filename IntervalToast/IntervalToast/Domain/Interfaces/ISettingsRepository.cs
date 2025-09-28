using IntervalToast.Domain.Entities;

namespace IntervalToast.Domain.Interfaces;

/// <summary>
/// Repository interface for application settings persistence
/// </summary>
public interface ISettingsRepository
{
    /// <summary>
    /// Loads the application settings
    /// </summary>
    Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the application settings
    /// </summary>
    Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a backup of the current settings
    /// </summary>
    Task<string> CreateBackupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores settings from a backup file
    /// </summary>
    Task<ApplicationSettings> RestoreFromBackupAsync(string backupFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the settings file integrity
    /// </summary>
    Task<bool> ValidateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets settings to defaults
    /// </summary>
    Task<ApplicationSettings> ResetToDefaultsAsync(CancellationToken cancellationToken = default);
}