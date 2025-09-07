using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;
using System.Text.Json;
using System.IO;
using Lazarus.Desktop.Services;
using Lazarus.Shared;
using Lazarus.Shared.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lazarus.Desktop.ViewModels;

public sealed class GlobalActionsViewModel : SettingsSectionBase
{
    public GlobalActionsViewModel(SettingsViewModel settings) : base(settings, "Global Actions")
    {
        CheckUpdatesCommand = new RelayCommand(async () =>
        {
            var svc = App.ServiceProvider.GetRequiredService<IUpdateService>();
            var result = await svc.CheckAsync().ConfigureAwait(false);
            var logger = App.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<GlobalActionsViewModel>>();
            if (result.IsAvailable)
                logger.LogInformation("Update available: {Latest} (current {Current})", result.Latest, result.Current);
            else
                logger.LogInformation("Up-to-date: {Current}", result.Current);
        });

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

        LoadLastModelCommand = new RelayCommand(async () =>
        {
            var settingsSvc = App.ServiceProvider.GetRequiredService<ISettingsService>();
            var path = settingsSvc.Current.ActiveModelId;
            if (string.IsNullOrWhiteSpace(path)) return;
            var client = App.ServiceProvider.GetRequiredService<IOrchestratorRunnerClient>();
            await client.LoadModelAsync(path!).ConfigureAwait(false);
        });

        UnloadRunnerCommand = new RelayCommand(async () =>
        {
            var client = App.ServiceProvider.GetRequiredService<IOrchestratorRunnerClient>();
            await client.UnloadAsync().ConfigureAwait(false);
        });

        OpenLogsFolderCommand = new RelayCommand(() => OpenFolder(LazarusPaths.FlatLogs));
        OpenSettingsFolderCommand = new RelayCommand(() => OpenFolder(SettingsPaths.AppDataRoot));
        OpenModelsFolderCommand = new RelayCommand(() => OpenFolder(LazarusPaths.Models.RootDir));

        ResetSettingsCommand = new RelayCommand(async () =>
        {
            var result = MessageBox.Show(
                "Reset all settings to defaults? This cannot be undone.",
                "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            var svc = App.ServiceProvider.GetRequiredService<ISettingsService>();
            // Reset to default schema and persist
            await svc.SaveAsync(AppSettings.CreateDefault()).ConfigureAwait(false);
            // SettingsViewModel is subscribed to SettingsChanged and will refresh UI
        });

        ExportSettingsCommand = new RelayCommand(() =>
        {
            try
            {
                var svc = App.ServiceProvider.GetRequiredService<ISettingsService>();
                var snapshot = svc.Current;

                var dlg = new SaveFileDialog
                {
                    Title = "Export Lazarus Settings",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    AddExtension = true,
                    DefaultExt = ".json",
                    OverwritePrompt = true,
                    FileName = $"lazarus-settings-{DateTime.Now:yyyyMMdd-HHmm}.json",
                    InitialDirectory = Directory.Exists(SettingsPaths.AppDataRoot)
                        ? SettingsPaths.AppDataRoot
                        : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };
                if (dlg.ShowDialog() != true) return;

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(snapshot, options);
                File.WriteAllText(dlg.FileName, json);
                MessageBox.Show($"Exported settings to:\n{dlg.FileName}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export settings:\n{ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });

        ImportSettingsCommand = new RelayCommand(async () =>
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Import Lazarus Settings",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    CheckFileExists = true,
                    CheckPathExists = true,
                };
                if (dlg.ShowDialog() != true) return;

                var json = File.ReadAllText(dlg.FileName);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                var imported = JsonSerializer.Deserialize<AppSettings>(json, options);
                if (imported is null)
                {
                    MessageBox.Show("Selected file does not contain valid Lazarus settings.", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var confirm = MessageBox.Show(
                    $"Import settings from:\n{dlg.FileName}\n\nThis will overwrite your current settings.",
                    "Confirm Import",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    var dir = Path.GetDirectoryName(SettingsPaths.SettingsFile)!;
                    Directory.CreateDirectory(dir);
                    if (File.Exists(SettingsPaths.SettingsFile))
                    {
                        File.Copy(SettingsPaths.SettingsFile, SettingsPaths.SettingsFile + ".bak", overwrite: true);
                    }
                }
                catch { }

                var svc = App.ServiceProvider.GetRequiredService<ISettingsService>();
                await svc.SaveAsync(imported).ConfigureAwait(false);
                MessageBox.Show("Settings imported successfully. Some changes may require an app restart.", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to import settings:\n{ex.Message}", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
    }

    public RelayCommand CheckUpdatesCommand { get; }
    public RelayCommand StartOrchestratorCommand { get; }
    public RelayCommand StopOrchestratorCommand { get; }
    public RelayCommand LoadLastModelCommand { get; }
    public RelayCommand UnloadRunnerCommand { get; }
    public RelayCommand OpenLogsFolderCommand { get; }
    public RelayCommand OpenSettingsFolderCommand { get; }
    public RelayCommand OpenModelsFolderCommand { get; }
    public RelayCommand ResetSettingsCommand { get; }
    public RelayCommand ExportSettingsCommand { get; }
    public RelayCommand ImportSettingsCommand { get; }

    private static void OpenFolder(string path)
    {
        try
        {
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
            var psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = path,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch { }
    }
}
