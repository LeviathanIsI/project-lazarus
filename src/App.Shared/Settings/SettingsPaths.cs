using System;
using System.IO;

namespace Lazarus.Shared.Settings;

/// <summary>
/// Provides standard paths for settings and application data
/// </summary>
public static class SettingsPaths
{

    /// <summary>
    /// Gets the root application data directory
    /// </summary>
    // Align settings with canonical LazarusPaths folder layout under %LOCALAPPDATA%\Lazarus
    public static string AppDataRoot => Lazarus.Shared.LazarusPaths.Root;

    /// <summary>
    /// Gets the settings file path
    /// </summary>
    public static string SettingsFile => Path.Combine(Lazarus.Shared.LazarusPaths.SystemData.Config, "settings.json");

    /// <summary>
    /// Gets the backup settings file path
    /// </summary>
    public static string SettingsBackupFile => Path.Combine(Lazarus.Shared.LazarusPaths.SystemData.Config, "settings.backup.json");

    /// <summary>
    /// Gets the models directory path
    /// </summary>
    public static string ModelsDirectory => Lazarus.Shared.LazarusPaths.Models.RootDir;

    /// <summary>
    /// Gets the cache directory path
    /// </summary>
    public static string CacheDirectory => Lazarus.Shared.LazarusPaths.SystemData.Cache;

    /// <summary>
    /// Gets the temporary files directory path
    /// </summary>
    public static string TempDirectory => Lazarus.Shared.LazarusPaths.SystemData.Cache;

    /// <summary>
    /// Gets the exports directory path
    /// </summary>
    public static string ExportsDirectory => Path.Combine(Lazarus.Shared.LazarusPaths.SharedResources.ImportExport, "Export");

    /// <summary>
    /// Gets the logs directory path
    /// </summary>
    public static string LogsDirectory => Lazarus.Shared.LazarusPaths.SystemData.Logs;

    /// <summary>
    /// Gets the database directory path
    /// </summary>
    public static string DatabaseDirectory => Lazarus.Shared.LazarusPaths.SystemData.Database;

    /// <summary>
    /// Ensures all required directories exist
    /// </summary>
    public static void EnsureDirectoriesExist()
    {
        // Use the canonical enumerator; already idempotent
        foreach (var d in Lazarus.Shared.LazarusPaths.EnumerateAllDirectories())
        {
            try { Directory.CreateDirectory(d); } catch { }
        }
        // Ensure configuration folder specifically for settings files
        try { Directory.CreateDirectory(Lazarus.Shared.LazarusPaths.SystemData.Config); } catch { }
    }

    /// <summary>
    /// Gets the size of a directory in bytes
    /// </summary>
    public static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path))
            return 0;

        var size = 0L;
        var di = new DirectoryInfo(path);

        try
        {
            foreach (var file in di.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                size += file.Length;
            }
        }
        catch
        {
            // Ignore access denied or other errors
        }

        return size;
    }

    /// <summary>
    /// Cleans old files from a directory
    /// </summary>
    public static void CleanOldFiles(string path, int daysToKeep)
    {
        if (!Directory.Exists(path))
            return;

        var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
        var di = new DirectoryInfo(path);

        try
        {
            foreach (var file in di.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if (file.LastWriteTime < cutoffDate)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                        // Ignore files that can't be deleted
                    }
                }
            }
        }
        catch
        {
            // Ignore access denied or other errors
        }
    }
}
