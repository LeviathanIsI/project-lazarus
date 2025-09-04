using Lazarus.App.Shared.Services;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;
using System.IO;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Implementation of view mode service for managing UI complexity levels
/// </summary>
public class ViewModeService : IViewModeService
{
    private readonly ILogger<ViewModeService> _logger;
    private readonly string _preferencesFilePath;
    private ViewMode _currentViewMode = ViewMode.Enthusiast; // Default to Enthusiast mode

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModeService"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public ViewModeService(ILogger<ViewModeService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Store preferences in the user's AppData folder
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "Lazarus");
        Directory.CreateDirectory(appFolder);
        _preferencesFilePath = Path.Combine(appFolder, "viewmode.json");
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public event EventHandler<ViewModeChangedEventArgs>? ViewModeChanged;

    /// <inheritdoc/>
    public ViewMode CurrentViewMode => _currentViewMode;

    /// <inheritdoc/>
    public bool ShowAdvancedFeatures => _currentViewMode >= ViewMode.Enthusiast;

    /// <inheritdoc/>
    public bool ShowDeveloperFeatures => _currentViewMode >= ViewMode.Developer;

    /// <inheritdoc/>
    public async Task SetViewModeAsync(ViewMode viewMode)
    {
        var previousMode = _currentViewMode;
        
        if (previousMode != viewMode)
        {
            _currentViewMode = viewMode;
            
            // Raise property changed events
            OnPropertyChanged(nameof(CurrentViewMode));
            OnPropertyChanged(nameof(ShowAdvancedFeatures));
            OnPropertyChanged(nameof(ShowDeveloperFeatures));
            
            // Raise view mode changed event
            ViewModeChanged?.Invoke(this, new ViewModeChangedEventArgs(previousMode, viewMode));
            
            // Save preference
            await SaveViewModeAsync();
            
            _logger.LogInformation("View mode changed from {PreviousMode} to {NewMode}", previousMode, viewMode);
        }
    }

    /// <inheritdoc/>
    public bool IsFeatureVisible(ViewMode requiredLevel)
    {
        return _currentViewMode >= requiredLevel;
    }

    /// <inheritdoc/>
    public async Task LoadViewModeAsync()
    {
        try
        {
            if (!File.Exists(_preferencesFilePath))
            {
                _logger.LogInformation("View mode preferences file not found. Using default mode: {DefaultMode}", _currentViewMode);
                await SaveViewModeAsync();
                return;
            }

            var jsonContent = await File.ReadAllTextAsync(_preferencesFilePath);
            var preference = JsonSerializer.Deserialize<ViewModePreference>(jsonContent);

            if (preference != null && Enum.IsDefined(typeof(ViewMode), preference.ViewMode))
            {
                var previousMode = _currentViewMode;
                _currentViewMode = preference.ViewMode;

                // Raise property changed events
                OnPropertyChanged(nameof(CurrentViewMode));
                OnPropertyChanged(nameof(ShowAdvancedFeatures));
                OnPropertyChanged(nameof(ShowDeveloperFeatures));

                _logger.LogInformation("View mode preference loaded: {ViewMode}", _currentViewMode);
            }
            else
            {
                _logger.LogWarning("Invalid view mode preference. Using default: {DefaultMode}", _currentViewMode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading view mode preferences from {FilePath}", _preferencesFilePath);
        }
    }

    /// <inheritdoc/>
    public async Task SaveViewModeAsync()
    {
        try
        {
            var preference = new ViewModePreference
            {
                ViewMode = _currentViewMode,
                LastModified = DateTime.UtcNow
            };

            var jsonContent = JsonSerializer.Serialize(preference, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_preferencesFilePath, jsonContent);
            
            _logger.LogDebug("View mode preference saved to {FilePath}: {ViewMode}", _preferencesFilePath, _currentViewMode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving view mode preferences to {FilePath}", _preferencesFilePath);
        }
    }

    /// <summary>
    /// Raises the PropertyChanged event
    /// </summary>
    /// <param name="propertyName">The name of the property that changed</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// View mode preference model for JSON serialization
    /// </summary>
    private class ViewModePreference
    {
        public ViewMode ViewMode { get; set; }
        public DateTime LastModified { get; set; }
    }
}