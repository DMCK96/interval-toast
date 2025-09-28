using IntervalToast.Domain.Entities;
using IntervalToast.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IntervalToast.Infrastructure.Persistence;

/// <summary>
/// JSON-based implementation of settings repository
/// </summary>
public sealed class JsonSettingsRepository : ISettingsRepository
{
    private const string SettingsFileName = "IntervalToast.Settings.json";
    private const string BackupFileExtension = ".backup";
    private const string ConfigurationFolderName = "IntervalToast";

    private readonly string _settingsDirectory;
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<JsonSettingsRepository> _logger;

    public JsonSettingsRepository(ILogger<JsonSettingsRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Setup paths
        _settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ConfigurationFolderName);
        _settingsFilePath = Path.Combine(_settingsDirectory, SettingsFileName);

        // Setup JSON options
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        // Ensure directory exists
        Directory.CreateDirectory(_settingsDirectory);
        _logger.LogDebug("Settings repository initialized at: {Path}", _settingsDirectory);
    }

    /// <inheritdoc />
    public async Task<IntervalToast.Domain.Entities.ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.LogInformation("Settings file not found, creating defaults");
                var defaultSettings = new IntervalToast.Domain.Entities.ApplicationSettings();
                await SaveAsync(defaultSettings, cancellationToken);
                return defaultSettings;
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath, cancellationToken);
            var settings = JsonSerializer.Deserialize<IntervalToast.Domain.Entities.ApplicationSettings>(json, _jsonOptions);

            if (settings == null)
            {
                _logger.LogWarning("Failed to deserialize settings, using defaults");
                return new IntervalToast.Domain.Entities.ApplicationSettings();
            }

            _logger.LogDebug("Settings loaded successfully");
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {Path}", _settingsFilePath);
            throw new InvalidOperationException($"Failed to load settings: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(IntervalToast.Domain.Entities.ApplicationSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            // Create backup of existing settings
            if (File.Exists(_settingsFilePath))
            {
                await CreateBackupAsync(cancellationToken);
            }

            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json, cancellationToken);

            _logger.LogDebug("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {Path}", _settingsFilePath);
            throw new InvalidOperationException($"Failed to save settings: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<string> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
                throw new FileNotFoundException("Settings file not found", _settingsFilePath);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"{Path.GetFileNameWithoutExtension(SettingsFileName)}_{timestamp}{BackupFileExtension}";
            var backupFilePath = Path.Combine(_settingsDirectory, backupFileName);

            await Task.Run(() => File.Copy(_settingsFilePath, backupFilePath), cancellationToken);

            _logger.LogInformation("Settings backup created: {BackupPath}", backupFilePath);
            return backupFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create settings backup");
            throw new InvalidOperationException($"Failed to create backup: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IntervalToast.Domain.Entities.ApplicationSettings> RestoreFromBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupFilePath);

        try
        {
            if (!File.Exists(backupFilePath))
                throw new FileNotFoundException("Backup file not found", backupFilePath);

            var json = await File.ReadAllTextAsync(backupFilePath, cancellationToken);
            var settings = JsonSerializer.Deserialize<IntervalToast.Domain.Entities.ApplicationSettings>(json, _jsonOptions);

            if (settings == null)
                throw new InvalidOperationException("Failed to deserialize backup file");

            await SaveAsync(settings, cancellationToken);

            _logger.LogInformation("Settings restored from backup: {BackupPath}", backupFilePath);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore settings from backup {BackupPath}", backupFilePath);
            throw new InvalidOperationException($"Failed to restore from backup: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
                return false;

            var json = await File.ReadAllTextAsync(_settingsFilePath, cancellationToken);
            var settings = JsonSerializer.Deserialize<IntervalToast.Domain.Entities.ApplicationSettings>(json, _jsonOptions);

            var isValid = settings != null;
            _logger.LogDebug("Settings validation result: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Settings validation failed");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IntervalToast.Domain.Entities.ApplicationSettings> ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Create backup of current settings if they exist
            if (File.Exists(_settingsFilePath))
            {
                await CreateBackupAsync(cancellationToken);
            }

            var defaultSettings = new ApplicationSettings();
            await SaveAsync(defaultSettings, cancellationToken);

            _logger.LogInformation("Settings reset to defaults");
            return defaultSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset settings to defaults");
            throw new InvalidOperationException($"Failed to reset settings: {ex.Message}", ex);
        }
    }
}