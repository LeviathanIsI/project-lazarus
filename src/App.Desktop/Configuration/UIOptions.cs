namespace Lazarus.Desktop.Configuration;

/// <summary>
/// Configuration options for UI behavior and preferences.
/// </summary>
public sealed class UIOptions
{
    public const string SectionName = "UI";

    /// <summary>
    /// Default theme to apply on startup.
    /// </summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>
    /// Initial view to display when the application starts.
    /// </summary>
    public string StartupView { get; set; } = "Dashboard";

    /// <summary>
    /// Interval for automatically saving user data and preferences.
    /// </summary>
    public TimeSpan AutoSaveInterval { get; set; } = TimeSpan.FromSeconds(30);
}