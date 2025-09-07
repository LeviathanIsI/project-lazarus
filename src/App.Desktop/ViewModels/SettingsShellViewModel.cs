using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.ViewModels;

public sealed class SettingsShellViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public ObservableCollection<SettingsSection> Sections { get; } = new();

    private SettingsSection? _selectedSection;
    public SettingsSection? SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (!ReferenceEquals(_selectedSection, value))
            {
                _selectedSection = value ?? Sections.FirstOrDefault();
                OnPropertyChanged();
            }
        }
    }

    public SettingsShellViewModel()
    {
        // Resolve one SettingsViewModel instance for all views to bind against
        var svm = App.ServiceProvider.GetRequiredService<SettingsViewModel>();

        SettingsSection Make<TView>(string title) where TView : UserControl, new()
        {
            var view = new TView();
            view.DataContext = svm;
            return new SettingsSection(title, view);
        }

        Sections.Add(Make<Views.GeneralSettingsView>("General"));
        Sections.Add(Make<Views.PathsSettingsView>("Paths"));
        Sections.Add(Make<Views.OrchestratorSettingsView>("Orchestrator"));
        Sections.Add(Make<Views.RunnersSettingsView>("Runners"));
        Sections.Add(Make<Views.ModelsSettingsView>("Models"));
        Sections.Add(Make<Views.AudioSettingsView>("Audio"));
        Sections.Add(Make<Views.RagSettingsView>("Embeddings / RAG"));
        Sections.Add(Make<Views.TrainingSettingsView>("Training"));
        Sections.Add(Make<Views.LoggingSettingsView>("Logging"));
        Sections.Add(Make<Views.AdvancedSettingsView>("Advanced"));
        Sections.Add(Make<Views.AvatarSettingsView>("Avatars (future)"));

        // Default selection
        SelectedSection = Sections.FirstOrDefault();
    }
}

public sealed record SettingsSection(string Title, UserControl View);
