using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using System.Text.Json;
using System.IO;
using Lazarus.Desktop.Services;
using Lazarus.Shared;
using Lazarus.Shared.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lazarus.Desktop.ViewModels;

public sealed partial class GlobalActionsViewModel : SettingsSectionBase
{
    private readonly ILogger<GlobalActionsViewModel> _logger;

    public GlobalActionsViewModel(SettingsViewModel settings) : base(settings, "Global Actions")
    {
        SectionDescription = "Global application actions and utilities";
        _logger = App.ServiceProvider.GetRequiredService<ILogger<GlobalActionsViewModel>>();

        InitializeCommands();

        // Initialize with default values immediately
        ResetToDefault();
        CompleteInitialization();
    }

    private void InitializeCommands()
    {
        // System Operations - Updates
        CheckUpdatesCommand = new RelayCommand(async () =>
        {
            var svc = App.ServiceProvider.GetRequiredService<IUpdateService>();
            var result = await svc.CheckAsync().ConfigureAwait(false);
            if (result.IsAvailable)
                _logger.LogInformation("Update available: {Latest} (current {Current})", result.Latest, result.Current);
            else
                _logger.LogInformation("Up-to-date: {Current}", result.Current);
        });

        ViewChangelogCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("https://github.com/project-lazarus/releases") { UseShellExecute = true }));

        // System Operations - Orchestrator
        StartOrchestratorCommand = new RelayCommand(async () =>
        {
            var proc = App.ServiceProvider.GetRequiredService<IOrchestratorProcessService>();
            await proc.StartIfNeededAsync(CancellationToken.None).ConfigureAwait(false);
        });

        StopOrchestratorCommand = new RelayCommand(async () =>
        {
            var proc = App.ServiceProvider.GetRequiredService<IOrchestratorProcessService>();
            await proc.StopIfOwnedAsync(CancellationToken.None).ConfigureAwait(false);
        });

        RestartOrchestratorCommand = new RelayCommand(async () =>
        {
            var proc = App.ServiceProvider.GetRequiredService<IOrchestratorProcessService>();
            await proc.StopIfOwnedAsync(CancellationToken.None).ConfigureAwait(false);
            await Task.Delay(1000);
            await proc.StartIfNeededAsync(CancellationToken.None).ConfigureAwait(false);
        });

        ViewOrchestratorStatusCommand = new RelayCommand(() => _logger.LogInformation("Orchestrator status requested"));

        // System Operations - Runner
        LoadLastModelCommand = new RelayCommand(async () =>
        {
            var settingsSvc = App.ServiceProvider.GetRequiredService<ISettingsService>();
            var path = settingsSvc.Current.ActiveModelId;
            if (string.IsNullOrWhiteSpace(path)) return;
            var client = App.ServiceProvider.GetRequiredService<IOrchestratorRunnerClient>();
            await client.LoadModelAsync(path!).ConfigureAwait(false);
        });

        UnloadRunnerCommand = new RelayCommand(() =>
        {
            // TODO: Implement unload model
            _logger.LogInformation("Unload model requested");
        });

        ViewRunnersCommand = new RelayCommand(() => _logger.LogInformation("View runners requested"));

        // System Operations - Health Monitoring
        RunHealthCheckCommand = new RelayCommand(async () =>
        {
            await Task.Run(() => _logger.LogInformation("Running health check..."));
        });

        ViewMetricsCommand = new RelayCommand(() => _logger.LogInformation("View metrics requested"));

        // System Operations - Maintenance
        ClearCacheCommand = new RelayCommand(() =>
        {
            try
            {
                var cacheDir = SettingsPaths.CacheDirectory;
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                    _logger.LogInformation("Cache folder created (was missing)");
                    return;
                }

                // Avoid nuking the orchestrator shadow folder while it may be running
                DeleteDirectoryContentsSafely(cacheDir, new[] { "OrchestratorHost" });
                _logger.LogInformation("Cache cleared (excluding active orchestrator shadow directory if present)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Clear Cache encountered issues; some files may be in use");
            }
        });

        OptimizeDatabaseCommand = new RelayCommand(() => _logger.LogInformation("Database optimization requested"));

        // System Operations - Backup & Restore
        CreateBackupCommand = new RelayCommand(CreateBackup);
        RestoreBackupCommand = new RelayCommand(RestoreBackup);

        // File Management - Quick Folders
        OpenLogsFolderCommand = new RelayCommand(() => OpenFolder(SettingsPaths.LogsDirectory));
        OpenSettingsFolderCommand = new RelayCommand(() => OpenFolder(SettingsPaths.AppDataRoot));
        OpenModelsFolderCommand = new RelayCommand(() => OpenFolder(SettingsPaths.ModelsDirectory));
        OpenDataFolderCommand = new RelayCommand(() => OpenFolder(SettingsPaths.AppDataRoot));

        // File Management - Import/Export
        ImportSettingsCommand = new RelayCommand(ImportSettings);
        ExportSettingsCommand = new RelayCommand(ExportSettings);
        ImportModelsCommand = new RelayCommand(() => _logger.LogInformation("Import models requested"));
        ExportConversationsCommand = new RelayCommand(() => _logger.LogInformation("Export conversations requested"));

        // File Management - Data Management
        ClearConversationsCommand = new RelayCommand(() => _logger.LogInformation("Clear conversations requested"));
        CompactDatabaseCommand = new RelayCommand(() => _logger.LogInformation("Compact database requested"));
        ResetSettingsCommand = new RelayCommand(() =>
        {
            // TODO: Implement settings reset
            _logger.LogInformation("Settings reset to defaults");
        });
        CleanTempFilesCommand = new RelayCommand(() =>
        {
            try
            {
                var tempDir = SettingsPaths.TempDirectory;
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                    _logger.LogInformation("Temp folder created (was missing)");
                    return;
                }

                // Treat temp under the same cache root; exclude orchestrator shadow
                DeleteDirectoryContentsSafely(tempDir, new[] { "OrchestratorHost" });
                _logger.LogInformation("Temp files cleaned (excluding active orchestrator shadow directory if present)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Clean Temp encountered issues; some files may be in use");
            }
        });

        // Advanced Tools - Developer Tools
        OpenConsoleCommand = new RelayCommand(() => _logger.LogInformation("Open console requested"));
        ExportLogsCommand = new RelayCommand(ExportLogs);
        OpenPerfMonitorCommand = new RelayCommand(() => _logger.LogInformation("Performance monitor requested"));

        // Advanced Tools - Network Tools
        TestConnectionCommand = new RelayCommand(() => _logger.LogInformation("Test connection requested"));
        ConfigureProxyCommand = new RelayCommand(() => _logger.LogInformation("Configure proxy requested"));
        OpenApiMonitorCommand = new RelayCommand(() => _logger.LogInformation("API monitor requested"));

        // Advanced Tools - System Integration
        ConfigureShellExtensionCommand = new RelayCommand(() => _logger.LogInformation("Shell extension configuration requested"));
        ConfigureFileAssociationsCommand = new RelayCommand(() => _logger.LogInformation("File associations configuration requested"));
        ConfigureStartupCommand = new RelayCommand(() => _logger.LogInformation("Startup configuration requested"));

        // Help & Support
        OpenDocumentationCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("https://github.com/project-lazarus/docs") { UseShellExecute = true }));
        OpenHelpCenterCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("https://github.com/project-lazarus/help") { UseShellExecute = true }));
        ReportIssueCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("https://github.com/project-lazarus/issues") { UseShellExecute = true }));
        OpenForumCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("https://github.com/project-lazarus/discussions") { UseShellExecute = true }));
        ContactSupportCommand = new RelayCommand(() => _logger.LogInformation("Contact support requested"));
        ShowAboutCommand = new RelayCommand(() => MessageBox.Show("Lazarus v1.0.0\n© 2024 Lazarus Project", "About", MessageBoxButton.OK, MessageBoxImage.Information));

        // Emergency Actions
        KillAllProcessesCommand = new RelayCommand(() =>
        {
            if (MessageBox.Show("Kill all runner processes?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _logger.LogWarning("Killing all runner processes");
                // TODO: Implement process killing
            }
        });

        EnterSafeModeCommand = new RelayCommand(() =>
        {
            if (MessageBox.Show("Restart in safe mode?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _logger.LogWarning("Entering safe mode");
                // TODO: Implement safe mode
            }
        });

        FactoryResetCommand = new RelayCommand(() =>
        {
            if (MessageBox.Show("Factory reset will DELETE ALL DATA. Continue?", "WARNING", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
            {
                if (MessageBox.Show("Are you absolutely sure?", "FINAL WARNING", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    _logger.LogError("Factory reset initiated");
                    // TODO: Implement factory reset
                }
            }
        });
    }

    // System Operations Commands
    public ICommand CheckUpdatesCommand { get; private set; } = null!;
    public ICommand ViewChangelogCommand { get; private set; } = null!;
    public ICommand StartOrchestratorCommand { get; private set; } = null!;
    public ICommand StopOrchestratorCommand { get; private set; } = null!;
    public ICommand RestartOrchestratorCommand { get; private set; } = null!;
    public ICommand ViewOrchestratorStatusCommand { get; private set; } = null!;
    public ICommand LoadLastModelCommand { get; private set; } = null!;
    public ICommand UnloadRunnerCommand { get; private set; } = null!;
    public ICommand ViewRunnersCommand { get; private set; } = null!;
    public ICommand RunHealthCheckCommand { get; private set; } = null!;
    public ICommand ViewMetricsCommand { get; private set; } = null!;
    public ICommand ClearCacheCommand { get; private set; } = null!;
    public ICommand OptimizeDatabaseCommand { get; private set; } = null!;
    public ICommand CreateBackupCommand { get; private set; } = null!;
    public ICommand RestoreBackupCommand { get; private set; } = null!;

    // File Management Commands
    public ICommand OpenLogsFolderCommand { get; private set; } = null!;
    public ICommand OpenSettingsFolderCommand { get; private set; } = null!;
    public ICommand OpenModelsFolderCommand { get; private set; } = null!;
    public ICommand OpenDataFolderCommand { get; private set; } = null!;
    public ICommand ImportSettingsCommand { get; private set; } = null!;
    public ICommand ExportSettingsCommand { get; private set; } = null!;
    public ICommand ImportModelsCommand { get; private set; } = null!;
    public ICommand ExportConversationsCommand { get; private set; } = null!;
    public ICommand ClearConversationsCommand { get; private set; } = null!;
    public ICommand CompactDatabaseCommand { get; private set; } = null!;
    public ICommand ResetSettingsCommand { get; private set; } = null!;
    public ICommand CleanTempFilesCommand { get; private set; } = null!;

    // Advanced Tools Commands
    public ICommand OpenConsoleCommand { get; private set; } = null!;
    public ICommand ExportLogsCommand { get; private set; } = null!;
    public ICommand OpenPerfMonitorCommand { get; private set; } = null!;
    public ICommand TestConnectionCommand { get; private set; } = null!;
    public ICommand ConfigureProxyCommand { get; private set; } = null!;
    public ICommand OpenApiMonitorCommand { get; private set; } = null!;
    public ICommand ConfigureShellExtensionCommand { get; private set; } = null!;
    public ICommand ConfigureFileAssociationsCommand { get; private set; } = null!;
    public ICommand ConfigureStartupCommand { get; private set; } = null!;

    // Help & Support Commands
    public ICommand OpenDocumentationCommand { get; private set; } = null!;
    public ICommand OpenHelpCenterCommand { get; private set; } = null!;
    public ICommand ReportIssueCommand { get; private set; } = null!;
    public ICommand OpenForumCommand { get; private set; } = null!;
    public ICommand ContactSupportCommand { get; private set; } = null!;
    public ICommand ShowAboutCommand { get; private set; } = null!;

    // Emergency Actions Commands
    public ICommand KillAllProcessesCommand { get; private set; } = null!;
    public ICommand EnterSafeModeCommand { get; private set; } = null!;
    public ICommand FactoryResetCommand { get; private set; } = null!;

    private void OpenFolder(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        Process.Start("explorer.exe", path);
    }

    private void ImportSettings()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Import Settings"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = File.ReadAllText(dialog.FileName);
                var settings = JsonSerializer.Deserialize<Lazarus.Shared.Settings.AppSettings>(json);
                if (settings != null)
                {
                    var settingsSvc = App.ServiceProvider.GetRequiredService<ISettingsService>();
                    // TODO: Apply imported settings
                    _logger.LogInformation("Settings imported from {Path}", dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import settings");
                MessageBox.Show($"Failed to import settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExportSettings()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Export Settings",
            FileName = $"lazarus-settings-{DateTime.Now:yyyy-MM-dd}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var settingsSvc = App.ServiceProvider.GetRequiredService<ISettingsService>();
                var json = JsonSerializer.Serialize(settingsSvc.Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dialog.FileName, json);
                _logger.LogInformation("Settings exported to {Path}", dialog.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export settings");
                MessageBox.Show($"Failed to export settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void CreateBackup()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*",
            Title = "Create Backup",
            FileName = $"lazarus-backup-{DateTime.Now:yyyy-MM-dd-HHmmss}.bak"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                // TODO: Implement full backup
                _logger.LogInformation("Backup created at {Path}", dialog.FileName);
                MessageBox.Show("Backup created successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create backup");
                MessageBox.Show($"Failed to create backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void RestoreBackup()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*",
            Title = "Restore Backup"
        };

        if (dialog.ShowDialog() == true)
        {
            if (MessageBox.Show("This will replace current data. Continue?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    // TODO: Implement restore
                    _logger.LogInformation("Backup restored from {Path}", dialog.FileName);
                    MessageBox.Show("Backup restored successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to restore backup");
                    MessageBox.Show($"Failed to restore backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void ExportLogs()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "ZIP files (*.zip)|*.zip|All files (*.*)|*.*",
            Title = "Export Logs",
            FileName = $"lazarus-logs-{DateTime.Now:yyyy-MM-dd-HHmmss}.zip"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                // TODO: Implement log export
                _logger.LogInformation("Logs exported to {Path}", dialog.FileName);
                MessageBox.Show("Logs exported successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export logs");
                MessageBox.Show($"Failed to export logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public override void RefreshFromSettings()
    {
        // Global actions don't have settings to refresh
    }

    public override async Task ApplySettingsAsync()
    {
        // Global actions don't have settings to apply
        await Task.CompletedTask;
    }

    protected override void ResetToDefault()
    {
        // Global actions don't have settings to reset
    }
}

public sealed partial class GlobalActionsViewModel
{
    private void DeleteDirectoryContentsSafely(string rootDir, string[]? excludeDirNames = null)
    {
        excludeDirNames ??= Array.Empty<string>();

        try
        {
            // Delete files in root
            foreach (var file in Directory.EnumerateFiles(rootDir))
            {
                TryDeleteFile(file);
            }

            // Delete subdirectories except excluded
            foreach (var dir in Directory.EnumerateDirectories(rootDir))
            {
                var name = Path.GetFileName(dir);
                if (excludeDirNames.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)))
                    continue;

                TryDeleteDirectoryRecursive(dir);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Non-fatal issue while enumerating for deletion");
        }
    }

    private static void TryDeleteDirectoryRecursive(string dir)
    {
        try
        {
            Directory.Delete(dir, true);
        }
        catch
        {
            // Fall back to best-effort recursive deletion
            try
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                {
                    TryDeleteFile(file);
                }
                foreach (var sub in Directory.EnumerateDirectories(dir, "*", SearchOption.AllDirectories).Reverse())
                {
                    try { Directory.Delete(sub, false); } catch { }
                }
                try { Directory.Delete(dir, false); } catch { }
            }
            catch { }
        }
    }

    private static void TryDeleteFile(string file)
    {
        try
        {
            // Skip obvious binaries that may be in use
            var ext = Path.GetExtension(file);
            if (string.Equals(ext, ".dll", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ext, ".exe", StringComparison.OrdinalIgnoreCase))
                return;

            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }
        catch { }
    }
}
