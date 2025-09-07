using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.ViewModels;

public sealed class SettingsShellViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // Holds view models; DataTemplates map VMs to views
    public ObservableCollection<object> Sections { get; } = new();

    private object? _selectedSectionVm;
    public object? SelectedSectionVm
    {
        get => _selectedSectionVm;
        set
        {
            if (!ReferenceEquals(_selectedSectionVm, value))
            {
                _selectedSectionVm = value ?? Sections.FirstOrDefault();
                OnPropertyChanged();
            }
        }
    }

    public SettingsShellViewModel()
    {
        var settings = App.ServiceProvider.GetRequiredService<SettingsViewModel>();

        Sections.Add(new GeneralSettingsViewModel(settings));
        Sections.Add(new PathsSettingsViewModel(settings));
        Sections.Add(new OrchestratorSettingsViewModel(settings));
        Sections.Add(new RunnersSettingsViewModel(settings));
        Sections.Add(new ModelsSettingsViewModel(settings));
        Sections.Add(new AudioSettingsViewModel(settings));
        Sections.Add(new RagSettingsViewModel(settings));
        Sections.Add(new TrainingSettingsViewModel(settings));
        Sections.Add(new LoggingSettingsViewModel(settings));
        Sections.Add(new AdvancedSettingsViewModel(settings));
        Sections.Add(new AvatarsSettingsViewModel(settings));

        SelectedSectionVm = Sections.FirstOrDefault();

        // Diagnostics to help verify DataTemplate wiring at runtime
        Debug.WriteLine($"[Settings] Sections count: {Sections.Count}");
        Debug.WriteLine($"[Settings] SelectedSectionVm = {SelectedSectionVm?.GetType().FullName}");
        Debug.WriteLine($"[Settings] Settings VM instance = {settings?.GetType().FullName}");
    }
}
