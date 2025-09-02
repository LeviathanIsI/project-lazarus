using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Service for managing user preferences including theme selection and persistence.
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Event raised when user preferences change.
    /// </summary>
    event EventHandler<UserPreferencesChangedEventArgs>? PreferencesChanged;

    /// <summary>
    /// Gets the current user preferences.
    /// </summary>
    UserPreferences Preferences { get; }

    /// <summary>
    /// Gets or sets the current theme preference.
    /// </summary>
    Theme CurrentTheme { get; set; }

    /// <summary>
    /// Loads user preferences from persistent storage.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LoadPreferencesAsync();

    /// <summary>
    /// Saves user preferences to persistent storage.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SavePreferencesAsync();

    /// <summary>
    /// Applies the current theme preference.
    /// </summary>
    void ApplyThemePreference();

    /// <summary>
    /// Resets preferences to default values.
    /// </summary>
    void ResetToDefaults();
}

/// <summary>
/// Implementation of user preferences service with JSON file persistence.
/// </summary>
public class UserPreferencesService : IUserPreferencesService
{
    private readonly ILogger<UserPreferencesService> _logger;
    private readonly string _preferencesFilePath;
    private UserPreferences _preferences;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserPreferencesService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public UserPreferencesService(ILogger<UserPreferencesService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Store preferences in the user's AppData folder
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "Lazarus");
        Directory.CreateDirectory(appFolder);
        _preferencesFilePath = Path.Combine(appFolder, "preferences.json");

        _preferences = new UserPreferences();
    }

    /// <inheritdoc/>
    public event EventHandler<UserPreferencesChangedEventArgs>? PreferencesChanged;

    /// <inheritdoc/>
    public UserPreferences Preferences => _preferences;

    /// <inheritdoc/>
    public Theme CurrentTheme
    {
        get => _preferences.Theme;
        set
        {
            if (_preferences.Theme != value)
            {
                var oldTheme = _preferences.Theme;
                _preferences.Theme = value;
                _preferences.LastModified = DateTime.UtcNow;

                _logger.LogInformation("Theme preference changed from {OldTheme} to {NewTheme}", oldTheme, value);

                PreferencesChanged?.Invoke(this, new UserPreferencesChangedEventArgs(
                    UserPreferenceChangeType.Theme, oldTheme, value));

                // Auto-save preferences when theme changes
                _ = Task.Run(SavePreferencesAsync);
            }
        }
    }

    /// <inheritdoc/>
    public async Task LoadPreferencesAsync()
    {
        try
        {
            if (!File.Exists(_preferencesFilePath))
            {
                _logger.LogInformation("Preferences file not found. Using default preferences.");
                _preferences = new UserPreferences();
                await SavePreferencesAsync();
                return;
            }

            var jsonContent = await File.ReadAllTextAsync(_preferencesFilePath);
            var loadedPreferences = JsonSerializer.Deserialize<UserPreferences>(jsonContent, GetJsonOptions());

            if (loadedPreferences != null)
            {
                _preferences = loadedPreferences;
                _logger.LogInformation("User preferences loaded successfully. Theme: {Theme}", _preferences.Theme);
            }
            else
            {
                _logger.LogWarning("Failed to deserialize preferences. Using defaults.");
                _preferences = new UserPreferences();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user preferences from {FilePath}", _preferencesFilePath);
            _preferences = new UserPreferences();
        }
    }

    /// <inheritdoc/>
    public async Task SavePreferencesAsync()
    {
        try
        {
            _preferences.LastModified = DateTime.UtcNow;

            var jsonContent = JsonSerializer.Serialize(_preferences, GetJsonOptions());
            await File.WriteAllTextAsync(_preferencesFilePath, jsonContent);

            _logger.LogDebug("User preferences saved successfully to {FilePath}", _preferencesFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user preferences to {FilePath}", _preferencesFilePath);
        }
    }

    /// <inheritdoc/>
    public void ApplyThemePreference()
    {
        try
        {
            ThemeManager.ApplyTheme(_preferences.Theme);
            _logger.LogInformation("Applied theme preference: {Theme}", _preferences.Theme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying theme preference: {Theme}", _preferences.Theme);
            
            // Fallback to dark theme if the preferred theme fails to load
            try
            {
                ThemeManager.ApplyTheme(Theme.Dark);
                _preferences.Theme = Theme.Dark;
                _logger.LogInformation("Fallback to Dark theme applied successfully");
            }
            catch (Exception fallbackEx)
            {
                _logger.LogCritical(fallbackEx, "Critical error: Failed to apply fallback theme");
            }
        }
    }

    /// <inheritdoc/>
    public void ResetToDefaults()
    {
        var oldPreferences = new UserPreferences
        {
            Theme = _preferences.Theme,
            AutoSavePreferences = _preferences.AutoSavePreferences,
            LastModified = _preferences.LastModified
        };

        _preferences = new UserPreferences();
        _logger.LogInformation("User preferences reset to defaults");

        PreferencesChanged?.Invoke(this, new UserPreferencesChangedEventArgs(
            UserPreferenceChangeType.Reset, oldPreferences, _preferences));

        // Auto-save after reset
        _ = Task.Run(SavePreferencesAsync);
    }

    /// <summary>
    /// Gets JSON serialization options for preferences persistence.
    /// </summary>
    /// <returns>JsonSerializerOptions configured for preferences.</returns>
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
    }
}

/// <summary>
/// User preferences model.
/// </summary>
public class UserPreferences
{
    /// <summary>
    /// Gets or sets the preferred theme.
    /// </summary>
    public Theme Theme { get; set; } = Theme.Dark;

    /// <summary>
    /// Gets or sets a value indicating whether preferences should be auto-saved.
    /// </summary>
    public bool AutoSavePreferences { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to remember window size and position.
    /// </summary>
    public bool RememberWindowState { get; set; } = true;

    /// <summary>
    /// Gets or sets the last window width.
    /// </summary>
    public double LastWindowWidth { get; set; } = 900;

    /// <summary>
    /// Gets or sets the last window height.
    /// </summary>
    public double LastWindowHeight { get; set; } = 600;

    /// <summary>
    /// Gets or sets the last window state (Normal, Maximized, Minimized).
    /// </summary>
    public string LastWindowState { get; set; } = "Normal";

    /// <summary>
    /// Gets or sets a value indicating whether to check for theme updates on startup.
    /// </summary>
    public bool CheckThemeUpdatesOnStartup { get; set; } = false;

    /// <summary>
    /// Gets or sets the timestamp when preferences were created.
    /// </summary>
    public DateTime Created { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when preferences were last modified.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the application version when preferences were last saved.
    /// </summary>
    public string? ApplicationVersion { get; set; }
}

/// <summary>
/// Event arguments for user preferences change notifications.
/// </summary>
public class UserPreferencesChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserPreferencesChangedEventArgs"/> class.
    /// </summary>
    /// <param name="changeType">The type of change that occurred.</param>
    /// <param name="oldValue">The old value (if applicable).</param>
    /// <param name="newValue">The new value (if applicable).</param>
    public UserPreferencesChangedEventArgs(UserPreferenceChangeType changeType, object? oldValue = null, object? newValue = null)
    {
        ChangeType = changeType;
        OldValue = oldValue;
        NewValue = newValue;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the type of preference change.
    /// </summary>
    public UserPreferenceChangeType ChangeType { get; }

    /// <summary>
    /// Gets the old value (if applicable).
    /// </summary>
    public object? OldValue { get; }

    /// <summary>
    /// Gets the new value (if applicable).
    /// </summary>
    public object? NewValue { get; }

    /// <summary>
    /// Gets the timestamp when the change occurred.
    /// </summary>
    public DateTime Timestamp { get; }
}

/// <summary>
/// Types of user preference changes.
/// </summary>
public enum UserPreferenceChangeType
{
    /// <summary>
    /// Theme preference changed.
    /// </summary>
    Theme,

    /// <summary>
    /// Window state preference changed.
    /// </summary>
    WindowState,

    /// <summary>
    /// Auto-save preference changed.
    /// </summary>
    AutoSave,

    /// <summary>
    /// Preferences were reset to defaults.
    /// </summary>
    Reset,

    /// <summary>
    /// General preference change.
    /// </summary>
    General
}