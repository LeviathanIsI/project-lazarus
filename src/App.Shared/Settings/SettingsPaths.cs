using System.IO;

namespace Lazarus.Shared.Settings;

/// <summary>
/// Provides canonical file system paths for settings persistence.
/// </summary>
public static class SettingsPaths
{
    /// <summary>
    /// Absolute path to the settings JSON file.
    /// %LOCALAPPDATA%\Lazarus\settings.json (or LAZARUS_HOME if set).
    /// </summary>
    public static string SettingsFile => Path.Combine(Lazarus.Shared.LazarusPaths.Root, "settings.json");

    /// <summary>
    /// Absolute path to a temporary settings file used during safe writes.
    /// </summary>
    public static string TempFile => SettingsFile + ".tmp";
}

