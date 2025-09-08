using System;
using System.IO;

namespace Lazarus.Shared.Settings;

/// <summary>
/// Provides standard paths for settings and application data
/// </summary>
public static class SettingsPaths
{
    private static readonly string CompanyName = "Lazarus";
    private static readonly string AppName = "LazarusAI";

    /// <summary>
    /// Gets the root application data directory
    /// </summary>
    public static string AppDataRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        CompanyName,
        AppName);

    /// <summary>
    /// Gets the settings file path
    /// </summary>
    public static string SettingsFile => Path.Combine(AppDataRoot, "settings.json");

    /// <summary>
    /// Gets the backup settings file path
    /// </summary>
    public static string SettingsBackupFile => Path.Combine(AppDataRoot, "settings.backup.json");

    /// <summary>
    /// Gets the models directory path
    /// </summary>
    public static string ModelsDirectory => Path.Combine(AppDataRoot, "Models");

    /// <summary>
    /// Gets the cache directory path
    /// </summary>
    public static string CacheDirectory => Path.Combine(AppDataRoot, "Cache");

    /// <summary>
    /// Gets the temporary files directory path
    /// </summary>
    public static string TempDirectory => Path.Combine(AppDataRoot, "Temp");

    /// <summary>
    /// Gets the exports directory path
    /// </summary>
    public static string ExportsDirectory => Path.Combine(AppDataRoot, "Exports");

    /// <summary>
    /// Gets the logs directory path
    /// </summary>
    public static string LogsDirectory => Path.Combine(AppDataRoot, "Logs");

    /// <summary>
    /// Gets the database directory path
    /// </summary>
    public static string DatabaseDirectory => Path.Combine(AppDataRoot, "Database");

    /// <summary>
    /// Ensures all required directories exist
    /// </summary>
    public static void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(AppDataRoot);
        Directory.CreateDirectory(ModelsDirectory);
        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(TempDirectory);
        Directory.CreateDirectory(ExportsDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(DatabaseDirectory);
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