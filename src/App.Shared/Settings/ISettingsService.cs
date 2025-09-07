using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lazarus.Shared.Settings;

/// <summary>
/// Contract for loading, persisting, and observing Lazarus application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the in-memory settings instance.
    /// </summary>
    AppSettings Current { get; }

    /// <summary>
    /// Gets the absolute file path of the persisted settings file.
    /// </summary>
    string SettingsFilePath { get; }

    /// <summary>
    /// Raised after settings are loaded, imported, reset, or updated.
    /// </summary>
    event EventHandler<AppSettings>? SettingsChanged;

    /// <summary>
    /// Loads settings from disk; creates defaults if missing.
    /// May apply schema upgrades.
    /// </summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves settings to disk immediately (safe write).
    /// </summary>
    Task SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a debounced save operation using the service's default debounce interval.
    /// </summary>
    void SaveSoon();

    /// <summary>
    /// Applies an update to the current settings and raises change notifications.
    /// Implementations may schedule a debounced save.
    /// </summary>
    void Update(Action<AppSettings> apply);

    /// <summary>
    /// Resets to default settings and persists them.
    /// </summary>
    Task ResetToDefaultsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports settings from a JSON file and persists them.
    /// </summary>
    Task ImportAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports current settings to a JSON file.
    /// </summary>
    Task ExportAsync(string filePath, CancellationToken cancellationToken = default);
}

