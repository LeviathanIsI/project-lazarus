using System;
using System.Windows;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Manages theme switching and resource dictionary management for the Lazarus application.
/// Provides static methods for applying themes and managing theme-related resources.
/// </summary>
public static class ThemeManager
{
    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    public static event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <summary>
    /// Gets the currently active theme.
    /// </summary>
    public static Theme CurrentTheme { get; private set; } = Theme.Dark;

    /// <summary>
    /// Dictionary of theme resource paths.
    /// </summary>
    private static readonly Dictionary<Theme, string> ThemeResourcePaths = new()
    {
        { Theme.Dark, "Resources/Themes/DarkTheme.xaml" },
        { Theme.Light, "Resources/Themes/LightTheme.xaml" },
        { Theme.Cyberpunk, "Resources/Themes/CyberpunkTheme.xaml" },
        { Theme.Minimal, "Resources/Themes/MinimalTheme.xaml" }
    };

    /// <summary>
    /// Dictionary of friendly theme names for display purposes.
    /// </summary>
    private static readonly Dictionary<Theme, string> ThemeDisplayNames = new()
    {
        { Theme.Dark, "Dark Gothic" },
        { Theme.Light, "Light Professional" },
        { Theme.Cyberpunk, "Cyberpunk Neon" },
        { Theme.Minimal, "Minimal Clean" }
    };

    /// <summary>
    /// Applies the specified theme to the application.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    /// <exception cref="ArgumentException">Thrown when an invalid theme is specified.</exception>
    /// <exception cref="InvalidOperationException">Thrown when theme resources cannot be loaded.</exception>
    public static void ApplyTheme(Theme theme)
    {
        try
        {
            if (!ThemeResourcePaths.ContainsKey(theme))
            {
                throw new ArgumentException($"Invalid theme specified: {theme}", nameof(theme));
            }

            var app = Application.Current;
            if (app?.Resources == null)
            {
                throw new InvalidOperationException("Application resources not available.");
            }

            // Get the resource dictionary path
            var resourcePath = ThemeResourcePaths[theme];
            var resourceUri = new Uri(resourcePath, UriKind.Relative);

            // Load the new theme resource dictionary
            var newThemeResources = new ResourceDictionary
            {
                Source = resourceUri
            };

            // Remove existing theme resources
            RemoveExistingThemeResources(app.Resources);

            // Add the new theme resources
            app.Resources.MergedDictionaries.Insert(0, newThemeResources);

            // Update current theme
            var previousTheme = CurrentTheme;
            CurrentTheme = theme;

            // Raise theme changed event
            ThemeChanged?.Invoke(null, new ThemeChangedEventArgs(previousTheme, theme));
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is InvalidOperationException))
        {
            throw new InvalidOperationException($"Failed to apply theme '{theme}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets all available themes.
    /// </summary>
    /// <returns>An enumerable of all available themes.</returns>
    public static IEnumerable<Theme> GetAvailableThemes()
    {
        return ThemeResourcePaths.Keys;
    }

    /// <summary>
    /// Gets the display name for a theme.
    /// </summary>
    /// <param name="theme">The theme to get the display name for.</param>
    /// <returns>The display name of the theme.</returns>
    public static string GetThemeDisplayName(Theme theme)
    {
        return ThemeDisplayNames.TryGetValue(theme, out var displayName) ? displayName : theme.ToString();
    }

    /// <summary>
    /// Gets the theme from its display name.
    /// </summary>
    /// <param name="displayName">The display name to convert.</param>
    /// <returns>The theme corresponding to the display name, or null if not found.</returns>
    public static Theme? GetThemeFromDisplayName(string displayName)
    {
        var kvp = ThemeDisplayNames.FirstOrDefault(kv => kv.Value.Equals(displayName, StringComparison.OrdinalIgnoreCase));
        return kvp.Equals(default(KeyValuePair<Theme, string>)) ? null : kvp.Key;
    }

    /// <summary>
    /// Checks if a theme resource is available.
    /// </summary>
    /// <param name="theme">The theme to check.</param>
    /// <returns>True if the theme resource is available; otherwise, false.</returns>
    public static bool IsThemeAvailable(Theme theme)
    {
        if (!ThemeResourcePaths.TryGetValue(theme, out var resourcePath))
        {
            return false;
        }

        try
        {
            var resourceUri = new Uri(resourcePath, UriKind.Relative);
            var testResources = new ResourceDictionary { Source = resourceUri };
            return testResources.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Refreshes the current theme by reapplying it.
    /// This can be useful after dynamic resource changes.
    /// </summary>
    public static void RefreshCurrentTheme()
    {
        var currentTheme = CurrentTheme;
        ApplyTheme(currentTheme);
    }

    /// <summary>
    /// Removes existing theme resource dictionaries from the application resources.
    /// </summary>
    /// <param name="resources">The application resource dictionary.</param>
    private static void RemoveExistingThemeResources(ResourceDictionary resources)
    {
        // Remove theme resource dictionaries by checking their source URIs
        var themeDictionaries = resources.MergedDictionaries
            .Where(dict => dict.Source != null && IsThemeResourcePath(dict.Source.OriginalString))
            .ToList();

        foreach (var themeDict in themeDictionaries)
        {
            resources.MergedDictionaries.Remove(themeDict);
        }
    }

    /// <summary>
    /// Checks if a resource path is a theme resource path.
    /// </summary>
    /// <param name="resourcePath">The resource path to check.</param>
    /// <returns>True if the path is a theme resource path; otherwise, false.</returns>
    private static bool IsThemeResourcePath(string resourcePath)
    {
        return ThemeResourcePaths.Values.Any(path => 
            resourcePath.Contains(path, StringComparison.OrdinalIgnoreCase) ||
            resourcePath.Contains("Themes/", StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Available themes for the Lazarus application.
/// </summary>
public enum Theme
{
    /// <summary>
    /// Dark gothic theme with crimson accents.
    /// </summary>
    Dark,

    /// <summary>
    /// Light professional theme with blue accents.
    /// </summary>
    Light,

    /// <summary>
    /// Cyberpunk theme with neon green and cyan chaos.
    /// </summary>
    Cyberpunk,

    /// <summary>
    /// Minimal clean monochrome theme.
    /// </summary>
    Minimal
}

/// <summary>
/// Event arguments for theme change notifications.
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeChangedEventArgs"/> class.
    /// </summary>
    /// <param name="previousTheme">The previously active theme.</param>
    /// <param name="newTheme">The newly active theme.</param>
    public ThemeChangedEventArgs(Theme previousTheme, Theme newTheme)
    {
        PreviousTheme = previousTheme;
        NewTheme = newTheme;
    }

    /// <summary>
    /// Gets the previously active theme.
    /// </summary>
    public Theme PreviousTheme { get; }

    /// <summary>
    /// Gets the newly active theme.
    /// </summary>
    public Theme NewTheme { get; }
}