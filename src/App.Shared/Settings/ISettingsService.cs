namespace Lazarus.Shared.Settings;

/// <summary>
/// Service interface for managing application settings
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current settings
    /// </summary>
    AppSettings Current { get; }

    /// <summary>
    /// Event raised when settings are changed
    /// </summary>
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Loads settings from storage
    /// </summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves settings to storage
    /// </summary>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets settings to defaults
    /// </summary>
    Task ResetToDefaultsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports settings to a JSON file
    /// </summary>
    Task ExportToJsonAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports settings from a JSON file
    /// </summary>
    Task ImportFromJsonAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates settings and returns any errors
    /// </summary>
    List<string> ValidateSettings(AppSettings settings);

    /// <summary>
    /// Gets a specific setting value
    /// </summary>
    T GetValue<T>(string key, T defaultValue);

    /// <summary>
    /// Sets a specific setting value
    /// </summary>
    Task SetValueAsync<T>(string key, T value, CancellationToken cancellationToken = default);
}

/// <summary>
/// Event arguments for settings changed events
/// </summary>
public class SettingsChangedEventArgs : EventArgs
{
    public AppSettings OldSettings { get; }
    public AppSettings NewSettings { get; }
    public List<string> ChangedProperties { get; }

    public SettingsChangedEventArgs(AppSettings oldSettings, AppSettings newSettings, List<string> changedProperties)
    {
        OldSettings = oldSettings;
        NewSettings = newSettings;
        ChangedProperties = changedProperties;
    }
}