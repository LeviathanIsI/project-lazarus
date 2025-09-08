using System.Reflection;
using System.Text.Json;
using Lazarus.Shared.Settings;
using Microsoft.Extensions.Logging;

namespace Lazarus.Backend.Services.Settings;

/// <summary>
/// Service implementation for managing application settings
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private AppSettings _current;
    private readonly JsonSerializerOptions _jsonOptions;

    public AppSettings Current => _current;

    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        _current = AppSettings.CreateDefault();
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Ensure directories exist
        SettingsPaths.EnsureDirectoriesExist();
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(SettingsPaths.SettingsFile))
            {
                var json = await File.ReadAllTextAsync(SettingsPaths.SettingsFile, cancellationToken);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                
                if (settings != null)
                {
                    var oldSettings = _current;
                    _current = settings;
                    
                    // Raise changed event
                    var changedProps = GetChangedProperties(oldSettings, _current);
                    if (changedProps.Any())
                    {
                        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(oldSettings, _current, changedProps));
                    }
                    
                    _logger.LogInformation("Settings loaded from {Path}", SettingsPaths.SettingsFile);
                    return _current;
                }
            }
            else
            {
                _logger.LogInformation("No settings file found, using defaults");
                await SaveAsync(_current, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            
            // Try to restore from backup
            if (File.Exists(SettingsPaths.SettingsBackupFile))
            {
                try
                {
                    var backupJson = await File.ReadAllTextAsync(SettingsPaths.SettingsBackupFile, cancellationToken);
                    var backupSettings = JsonSerializer.Deserialize<AppSettings>(backupJson, _jsonOptions);
                    if (backupSettings != null)
                    {
                        _current = backupSettings;
                        _logger.LogInformation("Settings restored from backup");
                        await SaveAsync(_current, cancellationToken);
                        return _current;
                    }
                }
                catch (Exception backupEx)
                {
                    _logger.LogError(backupEx, "Failed to restore from backup");
                }
            }
        }

        return _current;
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            // Validate settings
            var errors = settings.Validate();
            if (errors.Any())
            {
                _logger.LogWarning("Settings validation warnings: {Errors}", string.Join(", ", errors));
            }

            // Create backup of existing settings
            if (File.Exists(SettingsPaths.SettingsFile))
            {
                File.Copy(SettingsPaths.SettingsFile, SettingsPaths.SettingsBackupFile, true);
            }

            // Save new settings
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(SettingsPaths.SettingsFile, json, cancellationToken);

            var oldSettings = _current;
            _current = settings;

            // Raise changed event
            var changedProps = GetChangedProperties(oldSettings, _current);
            if (changedProps.Any())
            {
                SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(oldSettings, _current, changedProps));
            }

            _logger.LogInformation("Settings saved to {Path}", SettingsPaths.SettingsFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            throw;
        }
        finally
        {
            _saveLock.Release();
        }
    }

    public async Task ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var defaultSettings = AppSettings.CreateDefault();
        await SaveAsync(defaultSettings, cancellationToken);
        _logger.LogInformation("Settings reset to defaults");
    }

    public async Task ExportToJsonAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(_current, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
            _logger.LogInformation("Settings exported to {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export settings to {Path}", filePath);
            throw;
        }
    }

    public async Task ImportFromJsonAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
            
            if (settings == null)
            {
                throw new InvalidOperationException("Failed to deserialize settings from JSON");
            }

            // Validate imported settings
            var errors = settings.Validate();
            if (errors.Any())
            {
                _logger.LogWarning("Imported settings have validation warnings: {Errors}", string.Join(", ", errors));
            }

            await SaveAsync(settings, cancellationToken);
            _logger.LogInformation("Settings imported from {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import settings from {Path}", filePath);
            throw;
        }
    }

    public List<string> ValidateSettings(AppSettings settings)
    {
        return settings.Validate();
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        try
        {
            var property = typeof(AppSettings).GetProperty(key);
            if (property != null && property.PropertyType == typeof(T))
            {
                var value = property.GetValue(_current);
                return value != null ? (T)value : defaultValue;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get setting value for key {Key}", key);
        }

        return defaultValue;
    }

    public async Task SetValueAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        try
        {
            var property = typeof(AppSettings).GetProperty(key);
            if (property != null && property.CanWrite)
            {
                property.SetValue(_current, value);
                await SaveAsync(_current, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Property {Key} not found or not writable", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set setting value for key {Key}", key);
            throw;
        }
    }

    private List<string> GetChangedProperties(AppSettings oldSettings, AppSettings newSettings)
    {
        var changed = new List<string>();
        var properties = typeof(AppSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (prop.CanRead)
            {
                var oldValue = prop.GetValue(oldSettings);
                var newValue = prop.GetValue(newSettings);

                if (!Equals(oldValue, newValue))
                {
                    changed.Add(prop.Name);
                }
            }
        }

        return changed;
    }
}