using IntervalToast.Application.Interfaces;
using IntervalToast.Domain.Entities;
using IntervalToast.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;

namespace IntervalToast.Infrastructure.Services;

/// <summary>
/// Implementation of settings service
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _repository;
    private readonly ILogger<SettingsService> _logger;
    private ApplicationSettings? _cachedSettings;

    public SettingsService(ISettingsRepository repository, ILogger<SettingsService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ApplicationSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_cachedSettings == null)
            {
                _cachedSettings = await _repository.LoadAsync(cancellationToken);
                _logger.LogDebug("Settings loaded from repository");
            }

            return _cachedSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get settings");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateSettingsAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            await _repository.SaveAsync(settings, cancellationToken);
            _cachedSettings = settings;

            // Raise event
            SettingsUpdated?.Invoke(this, new SettingsUpdatedEventArgs(settings, DateTimeOffset.UtcNow));

            _logger.LogInformation("Settings updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update settings");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ApplicationSettings> ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var defaultSettings = await _repository.ResetToDefaultsAsync(cancellationToken);
            _cachedSettings = defaultSettings;

            // Raise event
            SettingsUpdated?.Invoke(this, new SettingsUpdatedEventArgs(defaultSettings, DateTimeOffset.UtcNow));

            _logger.LogInformation("Settings reset to defaults");
            return defaultSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset settings to defaults");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var backupPath = await _repository.CreateBackupAsync(cancellationToken);
            _logger.LogInformation("Settings backup created: {BackupPath}", backupPath);
            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create settings backup");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ApplicationSettings> RestoreFromBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupFilePath);

        try
        {
            var restoredSettings = await _repository.RestoreFromBackupAsync(backupFilePath, cancellationToken);
            _cachedSettings = restoredSettings;

            // Raise event
            SettingsUpdated?.Invoke(this, new SettingsUpdatedEventArgs(restoredSettings, DateTimeOffset.UtcNow));

            _logger.LogInformation("Settings restored from backup: {BackupPath}", backupFilePath);
            return restoredSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore settings from backup: {BackupPath}", backupFilePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<SettingsValidationResult> ValidateSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isValid = await _repository.ValidateAsync(cancellationToken);
            var errors = new List<string>();
            var warnings = new List<string>();
            var requiresBackup = false;

            if (!isValid)
            {
                errors.Add("Settings file is corrupted or invalid");
                requiresBackup = true;
            }

            // Additional validation logic can be added here
            var settings = await GetSettingsAsync(cancellationToken);

            // Validate hotkey bindings
            var duplicateHotkeys = settings.HotkeyBindings
                .GroupBy(h => h.HotkeyDefinition)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateHotkeys.Any())
            {
                errors.Add($"Duplicate hotkey bindings found: {duplicateHotkeys.Count}");
            }

            // Validate display settings
            if (settings.DisplaySettings.MaxConcurrentNotifications <= 0)
            {
                errors.Add("Maximum concurrent notifications must be greater than 0");
            }

            if (settings.DisplaySettings.DefaultDisplayDurationMs <= 0)
            {
                warnings.Add("Default display duration should be greater than 0");
            }

            _logger.LogDebug("Settings validation completed. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                errors.Count == 0, errors.Count, warnings.Count);

            return new SettingsValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings,
                RequiresBackup = requiresBackup
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during settings validation");
            return new SettingsValidationResult
            {
                IsValid = false,
                Errors = new List<string> { $"Validation failed: {ex.Message}" },
                RequiresBackup = true
            };
        }
    }

    /// <inheritdoc />
    public async Task ExportSettingsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            var settings = await GetSettingsAsync(cancellationToken);
            await _repository.SaveAsync(settings, cancellationToken);

            // Copy current settings file to export location
            await Task.Run(() => File.Copy(GetSettingsFilePath(), filePath, true), cancellationToken);

            _logger.LogInformation("Settings exported to: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export settings to: {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ApplicationSettings> ImportSettingsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Settings file not found", filePath);

            var importedSettings = await _repository.RestoreFromBackupAsync(filePath, cancellationToken);
            _cachedSettings = importedSettings;

            // Raise event
            SettingsUpdated?.Invoke(this, new SettingsUpdatedEventArgs(importedSettings, DateTimeOffset.UtcNow));

            _logger.LogInformation("Settings imported from: {FilePath}", filePath);
            return importedSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import settings from: {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc />
    public event EventHandler<SettingsUpdatedEventArgs>? SettingsUpdated;

    private string GetSettingsFilePath()
    {
        // This should match the path used in JsonSettingsRepository
        var settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IntervalToast");
        return Path.Combine(settingsDirectory, "IntervalToast.Settings.json");
    }
}