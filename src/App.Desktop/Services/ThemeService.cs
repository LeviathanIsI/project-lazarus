using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Implementation of the theme service for managing application visual themes.
/// </summary>
public sealed class ThemeService : IThemeService, IDisposable
{
    private readonly ILogger<ThemeService> _logger;
    private readonly Dictionary<string, string> _themeResourcePaths;
    private string _currentTheme = "Dark";
    private bool _disposed;

    public ThemeService(ILogger<ThemeService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize available themes with their resource paths
        _themeResourcePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Minimal", "Resources/Themes/MinimalTheme.xaml" },
            { "Light", "Resources/Themes/LightTheme.xaml" },
            { "Dark", "Resources/Themes/DarkTheme.xaml" },
            { "Cyberpunk", "Resources/Themes/CyberpunkTheme.xaml" }
        };

        AvailableThemes = _themeResourcePaths.Keys.ToArray();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public string CurrentTheme => _currentTheme;

    public IReadOnlyList<string> AvailableThemes { get; }

    public bool ApplyTheme(string themeName)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ThemeService));

        if (string.IsNullOrWhiteSpace(themeName))
        {
            _logger.LogWarning("Attempted to apply null or empty theme name");
            return false;
        }

        if (!_themeResourcePaths.TryGetValue(themeName, out var resourcePath))
        {
            _logger.LogWarning("Theme '{ThemeName}' not found in available themes", themeName);
            return false;
        }

        try
        {
            // Ensure we're on the UI thread
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                return Application.Current.Dispatcher.Invoke(() => ApplyTheme(themeName));
            }

            // Load the theme resource dictionary
            var themeUri = new Uri(resourcePath, UriKind.Relative);
            var themeDictionary = new ResourceDictionary { Source = themeUri };

            // Remove existing theme dictionaries
            var app = Application.Current;
            var existingThemeDict = app.Resources.MergedDictionaries
                .FirstOrDefault(dict => _themeResourcePaths.Values.Any(path =>
                    dict.Source?.OriginalString?.Contains(path.Replace("Resources/Themes/", "")) == true));

            if (existingThemeDict != null)
            {
                app.Resources.MergedDictionaries.Remove(existingThemeDict);
            }

            // Add the new theme dictionary
            app.Resources.MergedDictionaries.Add(themeDictionary);

            var previousTheme = _currentTheme;
            _currentTheme = themeName;

            _logger.LogInformation("Successfully applied theme '{ThemeName}'", themeName);

            // Notify of theme change
            OnThemeChanged(previousTheme, themeName);
            OnPropertyChanged(nameof(CurrentTheme));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply theme '{ThemeName}'", themeName);
            return false;
        }
    }

    public void RefreshTheme()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ThemeService));

        ApplyTheme(_currentTheme);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _logger.LogDebug("ThemeService disposed");
        }
    }

    private void OnThemeChanged(string previousTheme, string newTheme)
    {
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(previousTheme, newTheme));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}