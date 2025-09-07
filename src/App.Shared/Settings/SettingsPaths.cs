using System.IO;

namespace Lazarus.Shared.Settings;

/// <summary>
/// Provides canonical file system paths for settings persistence.
/// </summary>
public static class SettingsPaths
{
    /// <summary>
    /// %LOCALAPPDATA%\Lazarus root for app data.
    /// </summary>
    public static string AppDataRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lazarus");

    /// <summary>
    /// Absolute path to the settings JSON file.
    /// </summary>
    public static string SettingsFile => Path.Combine(AppDataRoot, "settings.json");

    /// <summary>
    /// Absolute path to a temporary settings file used during safe writes.
    /// </summary>
    public static string TempFile => SettingsFile + ".tmp";
}
