using System.ComponentModel;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Service for managing application themes and visual styling.
/// </summary>
public interface IThemeService : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the currently applied theme name.
    /// </summary>
    string CurrentTheme { get; }

    /// <summary>
    /// Gets the available theme names.
    /// </summary>
    IReadOnlyList<string> AvailableThemes { get; }

    /// <summary>
    /// Applies the specified theme to the application.
    /// </summary>
    /// <param name="themeName">The name of the theme to apply.</param>
    /// <returns>True if the theme was applied successfully; otherwise, false.</returns>
    bool ApplyTheme(string themeName);

    /// <summary>
    /// Reloads the current theme, useful for refreshing after resource changes.
    /// </summary>
    void RefreshTheme();

    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
}

/// <summary>
/// Event arguments for theme change events.
/// </summary>
public sealed class ThemeChangedEventArgs : EventArgs
{
    public ThemeChangedEventArgs(string previousTheme, string newTheme)
    {
        PreviousTheme = previousTheme;
        NewTheme = newTheme;
    }

    public string PreviousTheme { get; }
    public string NewTheme { get; }
}