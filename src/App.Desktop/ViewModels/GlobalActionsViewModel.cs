using System.Diagnostics;
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
    }

    public RelayCommand CheckUpdatesCommand { get; }
    public RelayCommand StartOrchestratorCommand { get; }
    public RelayCommand StopOrchestratorCommand { get; }
    public RelayCommand LoadLastModelCommand { get; }
    public RelayCommand UnloadRunnerCommand { get; }
    public RelayCommand OpenLogsFolderCommand { get; }
    public RelayCommand OpenSettingsFolderCommand { get; }
    public RelayCommand OpenModelsFolderCommand { get; }

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
